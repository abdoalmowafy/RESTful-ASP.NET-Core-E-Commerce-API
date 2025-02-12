using ECommerceAPI.Data;
using ECommerceAPI.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ECommerceAPI.DTOs.Responses;
using Microsoft.IdentityModel.Tokens;
using Mapster;
using ECommerceAPI.DTOs.Requests;

namespace ECommerceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrderController(DataContext context, PaymobService paymobService) : ControllerBase
    {
        private readonly DataContext _context = context;
        private readonly PaymobService _paymobService = paymobService;

        [HttpGet]
        public async Task<IActionResult> IndexOrders(int pageIndex = 1)
        {
            var user = await _context.Users
                .Include(u => u.Addresses)
                .Include(u => u.Orders)
                    .ThenInclude(o => o.OrderProducts)
                        .ThenInclude(op => op.Product)
                .Include(u => u.Orders)
                    .ThenInclude(o => o.PromoCode)
                .Include(u => u.Orders)
                    .ThenInclude(o => o.Address)
                .AsNoTracking()
                .FirstAsync(u => u.UserName == User.Identity!.Name);

            var orders = user.Orders
                .Where(o => o.Status != OrderStatus.Paying)
                .OrderByDescending(o => o.CreatedDateTime)
                .Select(o => o.Adapt<OrderResponse>())
                .ToPaginatedList(pageIndex, 10);

            return Ok(orders);
        }

        [HttpPost]
        public async Task<IActionResult> NewOrder([FromBody] OrderRequest request)
        {
            var user = await _context.Users
                .Include(u => u.Orders)
                .Include(u => u.Cart)
                    .ThenInclude(c => c.CartProducts)
                        .ThenInclude(cp => cp.Product)
                .Include(u => u.Cart)
                    .ThenInclude(c => c!.PromoCode)
                .Include(u => u.Addresses)
                .FirstAsync(u => u.UserName == User.Identity!.Name);

            if (!user.EmailConfirmed || !user.PhoneNumberConfirmed)
                return BadRequest("Confirm Email and Phone number first!");

            if (user.Orders.Any(o => o.Status != OrderStatus.Delivered && o.Status != OrderStatus.Deleted))
                return BadRequest("You have an ongoing order!");

            Cart cart = user.Cart!;
            var cartProducts = cart.CartProducts;
            var promo = cart.PromoCode;
            if (cartProducts.IsNullOrEmpty()
                || cartProducts.Any(cp => cp.Quantity < 1 || cp.Product.Quantity < cp.Quantity || cp.Product.DeletedDateTime.HasValue)
                || (promo is not null && !promo.Active))
                return BadRequest();

            var address = request.DeliveryNeeded ? 
                user.Addresses.FirstOrDefault(a => a.Id == request.AddressId) : 
                await _context.StoreAddresses.FindAsync(request.AddressId);
            if (address is null) return NotFound();

            OrderStatus orderStatus = OrderStatus.Paying;
            long Fee = request.DeliveryNeeded ? 5000L : 0L;
            if (request.PaymentMethod == PaymentMethod.COD)
            {
                Fee += 1000;
                orderStatus = OrderStatus.Processing;
            }

            // Creating Order Instance
            long totalCentsNoPromo = 0;
            var orderProducts = new List<OrderProduct>(cartProducts.Count);
            foreach (var cartProduct in cartProducts)
            {
                var orderProduct = new OrderProduct
                {
                    Product = cartProduct.Product,
                    ProductPriceCents = cartProduct.Product.PriceCents,
                    SalePercent = cartProduct.Product.SalePercent,
                    Quantity = cartProduct.Quantity,
                    WarrantyDays = cartProduct.Product.WarrantyDays,
                };
                totalCentsNoPromo += cartProduct.Product.PriceCents * cartProduct.Quantity * (100 - cartProduct.Product.SalePercent) / 100;
                _context.OrderProducts.Add(orderProduct);
                orderProducts.Add(orderProduct);
                cartProduct.Product.Quantity -= cartProduct.Quantity;
                _context.Products.Update(cartProduct.Product);
            }

            Order order = new()
            {
                UserId = user.Id,
                User = user,
                PromoCode = promo,
                PaymentMethod = request.PaymentMethod,
                Status = orderStatus,
                TotalCents = Fee + (promo is null ? totalCentsNoPromo
                : promo.MaxSaleCents is null ? totalCentsNoPromo * (100 - promo.Percent) / 100
                : totalCentsNoPromo - Math.Min(totalCentsNoPromo * promo.Percent / 100, promo.MaxSaleCents.Value)),
                DeliveryNeeded = request.DeliveryNeeded,
                OrderProducts = orderProducts,
                Address = address
            };

            // Processing Order
            if (request.PaymentMethod == PaymentMethod.COD)
            {
                _context.Orders.Add(order);
                user.Orders.Add(order);
                await _context.SaveChangesAsync();
                return Ok(order.Adapt<OrderResponse>());
            }
            else if (request.PaymentMethod == PaymentMethod.CreditCard)
            {
                var payment_url = await _paymobService.PayAsync(order, request.Identifier);
                return Ok(new { payment_url });
            }
            else
            {
                var payment_url = await _paymobService.PayAsync(order, request.Identifier);
                return Ok(new { payment_url });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var user = await _context.Users
                .Include(u => u.Orders)
                    .ThenInclude(o => o.OrderProducts)
                        .ThenInclude(op => op.Product)
                .FirstAsync(u => u.UserName == User.Identity!.Name);

            var order = User.IsInRole("Admin") || User.IsInRole("Moderator") ?
                await _context.Orders.Include(o => o.OrderProducts).ThenInclude(op => op.Product).FirstAsync(o => o.Id == id)
                : user.Orders.FirstOrDefault(o => o.Id == id);

            if (order is null) return NotFound();
            if (order.Status != OrderStatus.Processing) return BadRequest();

            // Delete order
            foreach (var orderProduct in order.OrderProducts)
            {
                orderProduct.Product.Quantity += orderProduct.Quantity;
                _context.Products.Update(orderProduct.Product);
            }
            order.DeletedDateTime = DateTime.Now;
            order.Status = OrderStatus.Deleted;
            _context.Orders.Update(order);
            _context.DeletesHistory.Add(new()
            {
                Deleter = user,
                DeletedType = nameof(Order),
                DeletedId = id
            });
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}

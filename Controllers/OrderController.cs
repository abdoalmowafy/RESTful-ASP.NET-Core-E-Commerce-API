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

namespace ECommerceAPI.Controllers
{
    [Route("api/[action]")]
    [ApiController]
    public class OrderController(DataContext context, IConfiguration configuration, HttpClient httpClient) : ControllerBase
    {
        private readonly DataContext _context = context;
        private readonly HttpClient _httpClient = httpClient;
        private readonly string ApiKey = configuration.GetSection("Paymob")["ApiKey"]!;
        private readonly int IntegrationId = int.Parse(configuration.GetSection("Paymob")["IntegrationId"]!);
        private readonly int IframeId = int.Parse(configuration.GetSection("Paymob")["Iframe1Id"]!);

        [Authorize, HttpGet]
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
                .Include(u => u.ReturnProductOrders)
                    .ThenInclude(rpo => rpo.Address)
                .Include(u => u.ReturnProductOrders)
                    .ThenInclude(rpo => rpo.Order)
                .Include(u => u.ReturnProductOrders)
                    .ThenInclude(rpo => rpo.OrderProduct)
                        .ThenInclude(op => op.Product)
                .AsNoTracking()
                .FirstAsync(u => u.UserName == User.Identity!.Name);

            var orders = user.Orders
                .Where(o => o.Status != OrderStatus.Paying)
                .OrderByDescending(o => o.CreatedDateTime)
                .Select(o => o.Adapt<OrderResponse>())
                .ToPaginatedList(pageIndex, 10);

            var returnProductOrders = user.ReturnProductOrders
                .OrderByDescending(o => o.CreatedDateTime)
                .Select(o => o.Adapt<ReturnProductOrderResponse>())
                .ToPaginatedList(pageIndex, 10);

            return Ok(new { orders, returnProductOrders });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> NewOrder(PaymentMethod paymentMethod, bool deliveryNeeded, int shippingAddressId, string? identifier)
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

            var address = deliveryNeeded ? user.Addresses.FirstOrDefault(a => a.Id == shippingAddressId) : await _context.StoreAddresses.FindAsync(shippingAddressId);
            if (address is null) return NotFound();

            OrderStatus orderStatus = OrderStatus.Paying;
            long Fee = deliveryNeeded ? 5000L : 0L;
            if (paymentMethod == PaymentMethod.COD)
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
                PaymentMethod = paymentMethod,
                Status = orderStatus,
                TotalCents = Fee + (promo is null ? totalCentsNoPromo
                : promo.MaxSaleCents is null ? totalCentsNoPromo * (100 - promo.Percent) / 100
                : totalCentsNoPromo - Math.Min(totalCentsNoPromo * promo.Percent / 100, promo.MaxSaleCents.Value)),
                DeliveryNeeded = deliveryNeeded,
                OrderProducts = orderProducts,
                Address = address
            };

            // Processing Order
            if (paymentMethod == PaymentMethod.COD)
            {
                _context.Orders.Add(order);
                user.Orders.Add(order);
                await _context.SaveChangesAsync();
                return Ok(order.Adapt<OrderResponse>());
            }
            else if (paymentMethod == PaymentMethod.CreditCard)
            {
                var PaymobService = new PaymobService(_httpClient, configuration);
                var payment_url = await PaymobService.PayAsync(order, identifier);
                return Ok(new { payment_url });
            }
            else
            {
                var PaymobService = new PaymobService(_httpClient, configuration);
                var payment_url = await PaymobService.PayAsync(order, identifier);
                return Ok(new { payment_url });
            }
        }

        [HttpDelete]
        [Authorize]
        public async Task<IActionResult> DeleteOrder(int orderId)
        {
            var user = await _context.Users
                .Include(u => u.Orders)
                    .ThenInclude(o => o.OrderProducts)
                        .ThenInclude(op => op.Product)
                .FirstAsync(u => u.UserName == User.Identity!.Name);

            var order = User.IsInRole("Admin") || User.IsInRole("Moderator") ?
                await _context.Orders.Include(o => o.OrderProducts).ThenInclude(op => op.Product).FirstAsync(o => o.Id == orderId)
                : user.Orders.FirstOrDefault(o => o.Id == orderId);

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
                DeletedId = orderId
            });
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> NewReturnProductOrder(int orderId, int orderProductId, bool deliveryNeeded, int addressId, string returnReason, int quantityToReturn)
        {
            if (string.IsNullOrWhiteSpace(returnReason) || quantityToReturn < 1) return BadRequest();

            var user = await _context.Users
                .Include(u => u.Addresses)
                .Include(u => u.Orders)
                    .ThenInclude(o => o.OrderProducts)
                        .ThenInclude(op => op.Product)
                .FirstAsync(u => u.UserName == User.Identity!.Name);

            var order = user.Orders.FirstOrDefault(o => o.Id == orderId);
            if (order is null) return NotFound();
            if (order.Status != OrderStatus.Delivered) return BadRequest();

            var orderProduct = order.OrderProducts.FirstOrDefault(op => op.Id == orderProductId);
            if (orderProduct is null) return NotFound();
            if (order.CreatedDateTime + TimeSpan.FromDays(orderProduct.WarrantyDays) < DateTime.Now) return BadRequest();

            var address = deliveryNeeded ? user.Addresses.FirstOrDefault(a => a.Id == addressId) : await _context.StoreAddresses.FindAsync(addressId);
            if (address is null) return NotFound();

            var returned = _context.ReturnProductOrders.Where(rpo => rpo.OrderProduct.Id == orderProduct.Id).Sum(rpo => rpo.Quantity);
            if (quantityToReturn > orderProduct.Quantity - returned) return BadRequest();

            var returnProductOrder = new ReturnProductOrder()
            {
                Order = order,
                Status = ReturnStatus.Processing,
                OrderProduct = orderProduct,
                Address = address,
                Quantity = quantityToReturn,
                ReturnReason = returnReason,
            };

            _context.ReturnProductOrders.Add(returnProductOrder);
            user.ReturnProductOrders.Add(returnProductOrder);
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Ok(returnProductOrder.Adapt<ReturnProductOrderResponse>());
        }

        [HttpDelete]
        [Authorize]
        public async Task<IActionResult> DeleteReturnProductOrder(int returnProductOrderId)
        {
            var user = await _context.Users
                .Include(u => u.ReturnProductOrders)
                .Include(u => u.Orders)
                    .ThenInclude(o => o.OrderProducts)
                        .ThenInclude(op => op.Product)
                .FirstAsync(u => u.UserName == User.Identity!.Name);
            var returnProductOrder = User.IsInRole("Admin") || User.IsInRole("Moderator") ?
                await _context.ReturnProductOrders.Include(rpo => rpo.Order).ThenInclude(o => o.OrderProducts).ThenInclude(op => op.Product).FirstAsync(rpo => rpo.Id == returnProductOrderId)
                : user.ReturnProductOrders.FirstOrDefault(rpo => rpo.Id == returnProductOrderId);

            if (returnProductOrder is null) return NotFound();

            if (returnProductOrder.Status == ReturnStatus.Returned || returnProductOrder.Status == ReturnStatus.Deleted)
                return BadRequest();

            returnProductOrder.DeletedDateTime = DateTime.Now;
            returnProductOrder.Status = ReturnStatus.Deleted;
            _context.ReturnProductOrders.Update(returnProductOrder);
            _context.DeletesHistory.Add(new()
            {
                Deleter = user,
                DeletedType = nameof(ReturnProductOrder),
                DeletedId = returnProductOrderId
            });
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}

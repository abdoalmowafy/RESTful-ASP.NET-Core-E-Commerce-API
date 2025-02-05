using ECommerceAPI.Data;
using ECommerceAPI.DTOs.Requests;
using ECommerceAPI.DTOs.Responses;
using ECommerceAPI.Models;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReturnController(DataContext context) : ControllerBase
    {
        private readonly DataContext _context = context;
        [HttpGet]
        public async Task<IActionResult> IndexReturns(int pageIndex = 1)
        {
            var user = await _context.Users
                .Include(u => u.Addresses)
                .Include(u => u.Returns)
                    .ThenInclude(rpo => rpo.Address)
                .Include(u => u.Returns)
                    .ThenInclude(rpo => rpo.Order)
                        .ThenInclude(o => o.OrderProducts)
                            .ThenInclude(op => op.Product)
                .Include(u => u.Returns)
                    .ThenInclude(rpo => rpo.OrderProduct)
                        .ThenInclude(op => op.Product)
                .AsNoTracking()
                .FirstAsync(u => u.UserName == User.Identity!.Name);

            var returns = user.Returns
                .OrderByDescending(o => o.CreatedDateTime)
                .Select(o => o.Adapt<ReturnResponse>())
                .ToPaginatedList(pageIndex, 10);

            return Ok(returns);
        }

        [HttpPost]
        public async Task<IActionResult> NewReturn([FromBody] ReturnRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ReturnReason) || request.QuantityToReturn < 1) return BadRequest();

            var user = await _context.Users
                .Include(u => u.Addresses)
                .Include(u => u.Orders)
                    .ThenInclude(o => o.OrderProducts)
                        .ThenInclude(op => op.Product)
                .FirstAsync(u => u.UserName == User.Identity!.Name);

            var order = user.Orders.FirstOrDefault(o => o.Id == request.OrderId);
            if (order is null) return NotFound();
            if (order.Status != OrderStatus.Delivered) return BadRequest();

            var orderProduct = order.OrderProducts.FirstOrDefault(op => op.Id == request.OrderProductId);
            if (orderProduct is null) return NotFound();
            if (order.CreatedDateTime + TimeSpan.FromDays(orderProduct.WarrantyDays) < DateTime.Now) return BadRequest();

            var address = request.DeliveryNeeded ? user.Addresses.FirstOrDefault(a => a.Id == request.AddressId) : await _context.StoreAddresses.FindAsync(request.AddressId);
            if (address is null) return NotFound();

            var returned = _context.Returns.Where(rpo => rpo.OrderProduct.Id == orderProduct.Id).Sum(rpo => rpo.Quantity);
            if (request.QuantityToReturn > orderProduct.Quantity - returned) return BadRequest();

            var returnOrder = new Return()
            {
                Order = order,
                Status = ReturnStatus.Processing,
                OrderProduct = orderProduct,
                Address = address,
                Quantity = request.QuantityToReturn,
                ReturnReason = request.ReturnReason,
            };

            _context.Returns.Add(returnOrder);
            user.Returns.Add(returnOrder);
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Ok(returnOrder.Adapt<ReturnResponse>());
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReturn(int id)
        {
            var user = await _context.Users
                .Include(u => u.Returns)
                .Include(u => u.Orders)
                    .ThenInclude(o => o.OrderProducts)
                        .ThenInclude(op => op.Product)
                .FirstAsync(u => u.UserName == User.Identity!.Name);
            var returnOrder = User.IsInRole("Admin") || User.IsInRole("Moderator") ?
                await _context.Returns.Include(rpo => rpo.Order).ThenInclude(o => o.OrderProducts).ThenInclude(op => op.Product).FirstAsync(rpo => rpo.Id == id)
                : user.Returns.FirstOrDefault(rpo => rpo.Id == id);

            if (returnOrder is null) return NotFound();

            if (returnOrder.Status == ReturnStatus.Returned || returnOrder.Status == ReturnStatus.Deleted)
                return BadRequest();

            returnOrder.DeletedDateTime = DateTime.Now;
            returnOrder.Status = ReturnStatus.Deleted;
            _context.Returns.Update(returnOrder);
            _context.DeletesHistory.Add(new()
            {
                Deleter = user,
                DeletedType = nameof(Return),
                DeletedId = id
            });
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}

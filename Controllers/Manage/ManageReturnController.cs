using ECommerceAPI.Data;
using ECommerceAPI.DTOs.Responses;
using ECommerceAPI.Models;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceAPI.Controllers.Manage
{
    [Route("api/manage/Return")]
    [ApiController]
    public class ManageReturnController(DataContext context) : ControllerBase
    {
        private readonly DataContext _context = context;

        [HttpGet]
        [Authorize(Roles = "Admin,Moderator,Transporter")]
        public async Task<IActionResult> ReturnsDashboard(string? targetUser, int? status, int pageIndex = 1)
        {
            var returnsQuery = _context.Returns
                .Include(rpo => rpo.Address)
                .Include(rpo => rpo.Transporter)
                .Include(rpo => rpo.Order)
                    .ThenInclude(o => o.User)
                .Include(rpo => rpo.OrderProduct)
                    .ThenInclude(op => op.Product)
                .AsNoTracking();

            if (!string.IsNullOrEmpty(targetUser))
                returnsQuery = returnsQuery.Where(rpo => rpo.Order.User.UserName == targetUser);

            if (status.HasValue)
                returnsQuery = returnsQuery.Where(rpo => rpo.Status == (ReturnStatus)status);

            if (User.IsInRole("Transporter"))
            {
                var user = await _context.Users.AsNoTracking().FirstAsync(u => u.UserName == User.Identity!.Name);
                returnsQuery = returnsQuery.Where(u => u.Transporter == user);
            }

            var returns = await returnsQuery.Select(o => o.Adapt<ReturnResponse>()).ToPaginatedListAsync(pageIndex, 20);

            return Ok(new
            {
                returns,
                targetUser,
                status
            });
        }

        [Authorize(Roles = "Admin,Moderator")]
        [HttpPatch("{transporterId}/{returnId}")]
        public async Task<IActionResult> AssignTransporterToReturn(int returnId, string transporterId)
        {
            var returnOrder = await _context.Returns
                .Include(o => o.Order)
                .Include(rpo => rpo.Transporter)
                .FirstOrDefaultAsync(rpo => rpo.Id == returnId);

            if (returnOrder is null) return NotFound("Return product order not found");

            var transporter = await _context.Users.AsNoTrackingWithIdentityResolution().FirstOrDefaultAsync(u => u.Id == transporterId);
            if (transporter is null) return NotFound("Transporter not found");

            if (returnOrder.Status != ReturnStatus.Processing) return BadRequest("Return product order is not in processing status");

            returnOrder.Transporter = transporter;
            returnOrder.Status = ReturnStatus.OnTheWay;
            _context.Returns.Update(returnOrder);
            await _context.SaveChangesAsync();

            return Ok(returnOrder.Adapt<ReturnResponse>());
        }

        [Authorize(Roles = "Admin,Moderator,Transporter")]
        [HttpPatch("{id}")]
        public async Task<IActionResult> Returned(int id)
        {
            var user = await _context.Users.AsNoTracking().FirstAsync(u => u.UserName == User.Identity!.Name);
            var returnOrder = await _context.Returns
                .Include(rpo => rpo.Transporter)
                .Include(rpo => rpo.Order)
                .Include(rpo => rpo.OrderProduct)
                    .ThenInclude(op => op.Product)
                .FirstOrDefaultAsync(rpo => rpo.Id == id);

            if (returnOrder is null) return NotFound("Return product order not found");

            var order = returnOrder.Order;
            var orderProduct = returnOrder.OrderProduct;
            var product = returnOrder.OrderProduct.Product;

            if (returnOrder.ReturnedDateTime is not null || returnOrder.DeletedDateTime is not null || order.Status != OrderStatus.Delivered
                || (User.IsInRole("Transporter") && returnOrder.Transporter!.Id != user.Id))
                return BadRequest("Invalid return product order state");

            product.Quantity += returnOrder.Quantity;
            _context.Products.Update(product);

            orderProduct.PartiallyOrFullyReturnedDateTime = DateTime.Now;
            _context.OrderProducts.Update(orderProduct);

            returnOrder.ReturnedDateTime = DateTime.Now;
            returnOrder.Status = ReturnStatus.Returned;
            _context.Returns.Update(returnOrder);

            await _context.SaveChangesAsync();

            return Ok(returnOrder.Adapt<ReturnResponse>());
        }
    }
}

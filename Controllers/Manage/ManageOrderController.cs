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
    [Route("api/manage/Order")]
    [ApiController]
    public class ManageOrderController(DataContext context) : ControllerBase
    {
        private readonly DataContext _context = context;

        [HttpGet]
        [Authorize(Roles = "Admin,Moderator,Transporter")]
        public async Task<IActionResult> OrdersDashboard(string? targetUser, int? status, int pageIndex = 1)
        {
            var ordersQuery = _context.Orders
                .Include(o => o.Transporter)
                .Include(o => o.Address)
                .Include(o => o.User)
                .Include(o => o.OrderProducts)
                    .ThenInclude(op => op.Product)
                .Where(o => o.Status != OrderStatus.Paying)
                .AsNoTracking();

            if (!string.IsNullOrEmpty(targetUser))
                ordersQuery = ordersQuery.Where(o => o.User.UserName == targetUser);

            if (status.HasValue)
                ordersQuery = ordersQuery.Where(o => o.Status == (OrderStatus)status + 1);

            if (User.IsInRole("Transporter"))
            {
                var user = await _context.Users.AsNoTracking().FirstAsync(u => u.UserName == User.Identity!.Name);
                ordersQuery = ordersQuery.Where(u => u.Transporter == user);
            }

            var orders = await ordersQuery.Select(o => o.Adapt<OrderResponse>()).ToPaginatedListAsync(pageIndex, 20);

            return Ok(new
            {
                orders,
                targetUser,
                status
            });
        }

        [Authorize(Roles = "Admin,Moderator")]
        [HttpPatch("{transporterId}/{orderId}")]
        public async Task<IActionResult> AssignTransporterToOrder(int orderId, string transporterId)
        {
            var order = await _context.Orders.Include(o => o.Transporter).FirstOrDefaultAsync(o => o.Id == orderId);
            if (order is null) return NotFound("Order not found");

            var transporter = await _context.Users.AsNoTrackingWithIdentityResolution().FirstOrDefaultAsync(t => t.Id == transporterId);
            if (transporter is null) return NotFound("Transporter not found");

            if (order.Status != OrderStatus.Processing) return BadRequest("Order is not in processing status");

            order.Transporter = transporter;
            order.TransporterId = transporterId;
            order.Status = OrderStatus.OnTheWay;
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();

            return Ok(order.Adapt<OrderResponse>());
        }

        [Authorize(Roles = "Admin,Moderator,Transporter")]
        [HttpPatch("{id}")]
        public async Task<IActionResult> Delivered(int id)
        {
            var user = await _context.Users.AsNoTracking().FirstAsync(u => u.UserName == User.Identity!.Name);
            var order = await _context.Orders.Include(o => o.Transporter).FirstOrDefaultAsync(o => o.Id == id);
            if (order is null) return NotFound("Order not found");

            if (order.Status != OrderStatus.OnTheWay || (User.IsInRole("Transporter") && order.Transporter!.Id != user.Id))
                return BadRequest("Order is not on the way or transporter mismatch");

            order.DeliveryDateTime = DateTime.Now;
            order.Status = OrderStatus.Delivered;
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();

            return Ok(order.Adapt<OrderResponse>());
        }
    }
}

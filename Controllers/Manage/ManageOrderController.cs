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
    [Route("api/manage/[action]")]
    [ApiController]
    public class ManageOrderController(DataContext context, UserManager<User> userManager) : ControllerBase
    {
        private readonly DataContext _context = context;
        private readonly UserManager<User> _userManager = userManager;

        [HttpGet]
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<IActionResult> GetTransporters()
        {
            var transporters = await _userManager.GetUsersInRoleAsync("Transporter");
            return Ok(transporters.Adapt<IList<UserDetailedResponse>>());
        }

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

            var returnProductOrdersQuery = _context.ReturnProductOrders
                .Include(rpo => rpo.Address)
                .Include(rpo => rpo.Transporter)
                .Include(rpo => rpo.Order)
                    .ThenInclude(o => o.User)
                .Include(rpo => rpo.OrderProduct)
                    .ThenInclude(op => op.Product)
                .AsNoTracking();

            if (!string.IsNullOrEmpty(targetUser))
            {
                ordersQuery = ordersQuery.Where(o => o.User.UserName == targetUser);
                returnProductOrdersQuery = returnProductOrdersQuery.Where(rpo => rpo.Order.User.UserName == targetUser);
            }

            if (status.HasValue)
            {
                ordersQuery = ordersQuery.Where(o => o.Status == (OrderStatus)status + 1);
                returnProductOrdersQuery = returnProductOrdersQuery.Where(rpo => rpo.Status == (ReturnStatus)status);
            }

            if (User.IsInRole("Transporter"))
            {
                var user = await _context.Users.AsNoTracking().FirstAsync(u => u.UserName == User.Identity!.Name);
                ordersQuery = ordersQuery.Where(u => u.Transporter == user);
                returnProductOrdersQuery = returnProductOrdersQuery.Where(u => u.Transporter == user);
            }

            var orders = await ordersQuery.Select(o => o.Adapt<OrderResponse>()).ToPaginatedListAsync(pageIndex, 20);
            var returnProductOrders = await returnProductOrdersQuery.Select(o => o.Adapt<ReturnProductOrderResponse>()).ToPaginatedListAsync(pageIndex, 20);

            return Ok(new
            {
                orders,
                returnProductOrders,
                targetUser,
                status
            });
        }

        [Authorize(Roles = "Admin,Moderator")]
        [HttpPut]
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

        [Authorize(Roles = "Admin,Moderator")]
        [HttpPut]
        public async Task<IActionResult> AssignTransporterToReturnProductOrder(int returnProductOrderId, string transporterId)
        {
            var returnProductOrder = await _context.ReturnProductOrders
                .Include(o => o.Order)
                .Include(rpo => rpo.Transporter)
                .FirstOrDefaultAsync(rpo => rpo.Id == returnProductOrderId);

            if (returnProductOrder is null) return NotFound("Return product order not found");

            var transporter = await _context.Users.AsNoTrackingWithIdentityResolution().FirstOrDefaultAsync(u => u.Id == transporterId);
            if (transporter is null) return NotFound("Transporter not found");

            if (returnProductOrder.Status != ReturnStatus.Processing) return BadRequest("Return product order is not in processing status");

            returnProductOrder.Transporter = transporter;
            returnProductOrder.Status = ReturnStatus.OnTheWay;
            _context.ReturnProductOrders.Update(returnProductOrder);
            await _context.SaveChangesAsync();

            return Ok(returnProductOrder.Adapt<ReturnProductOrderResponse>());
        }

        [Authorize(Roles = "Admin,Moderator,Transporter")]
        [HttpPut]
        public async Task<IActionResult> Delivered(int orderId)
        {
            var user = await _context.Users.AsNoTracking().FirstAsync(u => u.UserName == User.Identity!.Name);
            var order = await _context.Orders.Include(o => o.Transporter).FirstOrDefaultAsync(o => o.Id == orderId);
            if (order is null) return NotFound("Order not found");

            if (order.Status != OrderStatus.OnTheWay || (User.IsInRole("Transporter") && order.Transporter!.Id != user.Id))
                return BadRequest("Order is not on the way or transporter mismatch");

            order.DeliveryDateTime = DateTime.Now;
            order.Status = OrderStatus.Delivered;
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();

            return Ok(order.Adapt<OrderResponse>());
        }

        [Authorize(Roles = "Admin,Moderator,Transporter")]
        [HttpPut]
        public async Task<IActionResult> Returned(int returnProductOrderId)
        {
            var user = await _context.Users.AsNoTracking().FirstAsync(u => u.UserName == User.Identity!.Name);
            var returnProductOrder = await _context.ReturnProductOrders
                .Include(rpo => rpo.Transporter)
                .Include(rpo => rpo.Order)
                .Include(rpo => rpo.OrderProduct)
                    .ThenInclude(op => op.Product)
                .FirstOrDefaultAsync(rpo => rpo.Id == returnProductOrderId);

            if (returnProductOrder is null) return NotFound("Return product order not found");

            var order = returnProductOrder.Order;
            var orderProduct = returnProductOrder.OrderProduct;
            var product = returnProductOrder.OrderProduct.Product;

            if (returnProductOrder.ReturnedDateTime is not null || returnProductOrder.DeletedDateTime is not null || order.Status != OrderStatus.Delivered
                || (User.IsInRole("Transporter") && returnProductOrder.Transporter!.Id != user.Id))
                return BadRequest("Invalid return product order state");

            product.Quantity += returnProductOrder.Quantity;
            _context.Products.Update(product);

            orderProduct.PartiallyOrFullyReturnedDateTime = DateTime.Now;
            _context.OrderProducts.Update(orderProduct);

            returnProductOrder.ReturnedDateTime = DateTime.Now;
            returnProductOrder.Status = ReturnStatus.Returned;
            _context.ReturnProductOrders.Update(returnProductOrder);

            await _context.SaveChangesAsync();

            return Ok(returnProductOrder.Adapt<ReturnProductOrderResponse>());
        }
    }
}

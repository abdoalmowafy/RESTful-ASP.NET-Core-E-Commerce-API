using Ordering.Management.Contracts;
using Ordering.Management.Services;

namespace Ordering.Management.Controllers;

[Route("admin/orders")]
[ApiController]
[Authorize]
public class OrdersController(IOrderManagementService orderService) : ControllerBase
{
    private readonly IOrderManagementService _orderService = orderService;

    [HttpGet]
    [HasPermission(Permissions.Orders.View)]
    public async Task<IActionResult> Get(
        [FromQuery] OrderStatus? status,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _orderService.GetAsync(status, pageIndex, pageSize, cancellationToken);
        return result.IsSucceed ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPatch("{id:int}/status")]
    [HasPermission(Permissions.Orders.Update)]
    public async Task<IActionResult> UpdateStatus(
        [FromRoute] int id,
        [FromBody] UpdateOrderStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _orderService.UpdateStatusAsync(id, request, cancellationToken);
        return result.IsSucceed ? NoContent() : result.ToProblem();
    }

    [HttpPatch("{id:int}/transporter")]
    [HasPermission(Permissions.Orders.Update)]
    public async Task<IActionResult> AssignTransporter(
        [FromRoute] int id,
        [FromBody] AssignTransporterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _orderService.AssignTransporterAsync(id, request, cancellationToken);
        return result.IsSucceed ? NoContent() : result.ToProblem();
    }
}

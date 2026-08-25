using Ordering.Management.Contracts;
using Ordering.Management.Services;

namespace Ordering.Management.Controllers;

[Route("admin/returns")]
[ApiController]
[Authorize]
public class ReturnsController(IReturnManagementService returnService) : ControllerBase
{
    private readonly IReturnManagementService _returnService = returnService;

    [HttpGet]
    [HasPermission(Permissions.Returns.View)]
    public async Task<IActionResult> Get(
        [FromQuery] ReturnStatus? status,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _returnService.GetAsync(status, pageIndex, pageSize, cancellationToken);
        return result.IsSucceed ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPatch("{id:int}/status")]
    [HasPermission(Permissions.Returns.Manage)]
    public async Task<IActionResult> UpdateStatus(
        [FromRoute] int id,
        [FromBody] UpdateReturnStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _returnService.UpdateStatusAsync(id, request, cancellationToken);
        return result.IsSucceed ? NoContent() : result.ToProblem();
    }

    [HttpPatch("{id:int}/transporter")]
    [HasPermission(Permissions.Returns.Manage)]
    public async Task<IActionResult> AssignTransporter(
        [FromRoute] int id,
        [FromBody] AssignTransporterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _returnService.AssignTransporterAsync(id, request, cancellationToken);
        return result.IsSucceed ? NoContent() : result.ToProblem();
    }
}

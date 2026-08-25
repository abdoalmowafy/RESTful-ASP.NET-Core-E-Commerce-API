using Driver.Management.Contracts;
using Driver.Management.Services;

namespace Driver.Management.Controllers;

[Route("admin/drivers")]
[ApiController]
[Authorize]
public class DriversController(IDriverManagementService driverService) : ControllerBase
{
    private readonly IDriverManagementService _driverService = driverService;

    [HttpGet]
    [HasPermission(Permissions.Drivers.View)]
    public async Task<IActionResult> Get(
        [FromQuery] DriverStatus? status,
        [FromQuery] string? search,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _driverService.GetAsync(status, search, pageIndex, pageSize, cancellationToken);
        return result.IsSucceed ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPatch("{driverId}/status")]
    [HasPermission(Permissions.Drivers.Manage)]
    public async Task<IActionResult> UpdateStatus(
        [FromRoute] string driverId,
        [FromBody] UpdateDriverStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _driverService.UpdateStatusAsync(driverId, request, cancellationToken);
        return result.IsSucceed ? NoContent() : result.ToProblem();
    }
}

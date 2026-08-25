using Driver.Management.Contracts;
using Driver.Management.Services;
using Driver.Profile.Contracts;
using Driver.Profile.Services;

namespace Driver.Management.Controllers;

[Route("admin/driver-requests")]
[ApiController]
[Authorize]
public class DriverRequestsController(
    IDriverApplicationService applicationService,
    IDriverManagementService driverManagementService) : ControllerBase
{
    private readonly IDriverApplicationService _applicationService = applicationService;
    private readonly IDriverManagementService _driverManagementService = driverManagementService;

    [HttpGet]
    [HasPermission(Permissions.Drivers.View)]
    public async Task<IActionResult> GetPendingRequests(CancellationToken cancellationToken)
    {
        var result = await _applicationService.GetPendingRequestsAsync(cancellationToken);
        return result.IsSucceed ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPatch("{driverId}/status")]
    [HasPermission(Permissions.Drivers.Manage)]
    public async Task<IActionResult> UpdateStatus(
        [FromRoute] string driverId,
        [FromBody] UpdateDriverStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _driverManagementService.UpdateStatusAsync(driverId, request, cancellationToken);
        return result.IsSucceed ? NoContent() : result.ToProblem();
    }
}

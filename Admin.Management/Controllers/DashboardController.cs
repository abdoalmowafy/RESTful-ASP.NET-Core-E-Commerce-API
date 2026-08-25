using Admin.Management.Contracts;
using Admin.Management.Services;

namespace Admin.Management.Controllers;

[Route("admin/dashboard")]
[ApiController]
[Authorize]
public class DashboardController(IDashboardService dashboardService) : ControllerBase
{
    private readonly IDashboardService _dashboardService = dashboardService;

    [HttpGet]
    [HasPermission(Permissions.Orders.View)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetAsync(cancellationToken);
        return result.IsSucceed ? Ok(result.Value) : result.ToProblem();
    }
}

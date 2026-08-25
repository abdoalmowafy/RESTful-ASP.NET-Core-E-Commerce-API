using Admin.Management.Contracts;
using Admin.Management.Services;

namespace Admin.Management.Controllers;

[Route("admin/admins")]
[ApiController]
[Authorize]
public class AdminsController(IAdminAccountsService accountsService) : ControllerBase
{
    private readonly IAdminAccountsService _accountsService = accountsService;

    [HttpGet]
    [HasPermission(Permissions.Admins.View)]
    public async Task<IActionResult> Get(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _accountsService.GetAsync(pageIndex, pageSize, cancellationToken);
        return result.IsSucceed ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPost]
    [HasPermission(Permissions.Admins.Manage)]
    public async Task<IActionResult> Create([FromBody] CreateAdminRequest request, CancellationToken cancellationToken)
    {
        var result = await _accountsService.CreateAsync(request, cancellationToken);
        return result.IsSucceed ? StatusCode(StatusCodes.Status201Created) : result.ToProblem();
    }

    [HttpPatch("{adminId}/status")]
    [HasPermission(Permissions.Admins.Manage)]
    public async Task<IActionResult> SetStatus(
        [FromRoute] string adminId,
        [FromBody] UpdateAdminStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _accountsService.SetStatusAsync(adminId, request, cancellationToken);
        return result.IsSucceed ? NoContent() : result.ToProblem();
    }
}

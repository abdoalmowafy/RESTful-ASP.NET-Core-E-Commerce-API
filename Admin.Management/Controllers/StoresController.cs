using Admin.Management.Contracts;
using Admin.Management.Services;

namespace Admin.Management.Controllers;

[Route("admin/stores")]
[ApiController]
[Authorize]
public class StoresController(IStoreManagementService storeService) : ControllerBase
{
    private readonly IStoreManagementService _storeService = storeService;

    [HttpGet]
    [HasPermission(Permissions.Stores.View)]
    public async Task<IActionResult> Get(
        [FromQuery] StoreStatus? status,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _storeService.GetAsync(status, pageIndex, pageSize, cancellationToken);
        return result.IsSucceed ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPatch("{storeId:int}/status")]
    [HasPermission(Permissions.Stores.Manage)]
    public async Task<IActionResult> UpdateStatus(
        [FromRoute] int storeId,
        [FromBody] UpdateStoreStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _storeService.UpdateStatusAsync(storeId, request, cancellationToken);
        return result.IsSucceed ? NoContent() : result.ToProblem();
    }
}

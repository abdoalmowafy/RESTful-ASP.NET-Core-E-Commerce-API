using Admin.Management.Services;
using Seller.Management.Contracts;
using Seller.Management.Services;

namespace Seller.Management.Controllers;

[Route("admin/sellers")]
[ApiController]
[Authorize]
public class SellersController(
    ISellerManagementService sellerService,
    IStoreManagementService storeManagementService) : ControllerBase
{
    private readonly ISellerManagementService _sellerService = sellerService;
    private readonly IStoreManagementService _storeManagementService = storeManagementService;

    [HttpGet]
    [HasPermission(Permissions.Sellers.View)]
    public async Task<IActionResult> Get(
        [FromQuery] StoreStatus? status,
        [FromQuery] string? search,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _sellerService.GetAsync(status, search, pageIndex, pageSize, cancellationToken);
        return result.IsSucceed ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPatch("{storeId:int}/status")]
    [HasPermission(Permissions.Sellers.Manage)]
    public async Task<IActionResult> UpdateStatus(
        [FromRoute] int storeId,
        [FromBody] UpdateSellerStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _storeManagementService.UpdateStatusAsync(
            storeId,
            new Admin.Management.Contracts.UpdateStoreStatusRequest(request.Status, request.Reason),
            cancellationToken);

        return result.IsSucceed ? NoContent() : result.ToProblem();
    }
}

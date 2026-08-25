using Catalog.Management.Contracts;
using Catalog.Management.Services;

namespace Catalog.Management.Controllers;

[Route("admin/store-addresses")]
[ApiController]
[Authorize]
public class StoreAddressesController(IStoreAddressManagementService storeAddressService) : ControllerBase
{
    private readonly IStoreAddressManagementService _storeAddressService = storeAddressService;

    [HttpGet]
    [HasPermission(Permissions.StoreAddresses.Manage)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await _storeAddressService.GetAsync(cancellationToken);
        return result.IsSucceed ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPost]
    [HasPermission(Permissions.StoreAddresses.Manage)]
    public async Task<IActionResult> Create([FromBody] StoreAddressRequest request, CancellationToken cancellationToken)
    {
        var result = await _storeAddressService.CreateAsync(request, cancellationToken);
        return result.IsSucceed ? CreatedAtAction(nameof(Get), result.Value) : result.ToProblem();
    }

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.StoreAddresses.Manage)]
    public async Task<IActionResult> Update(
        [FromRoute] int id,
        [FromBody] StoreAddressRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _storeAddressService.UpdateAsync(id, request, cancellationToken);
        return result.IsSucceed ? Ok(result.Value) : result.ToProblem();
    }

    [HttpDelete("{id:int}")]
    [HasPermission(Permissions.StoreAddresses.Manage)]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await _storeAddressService.DeleteAsync(id, User, cancellationToken);
        return result.IsSucceed ? NoContent() : result.ToProblem();
    }
}

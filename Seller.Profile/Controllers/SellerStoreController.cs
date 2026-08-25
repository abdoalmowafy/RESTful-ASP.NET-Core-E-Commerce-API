using Seller.Profile.Contracts;
using Seller.Profile.Services;

namespace Seller.Profile.Controllers;

[Route("seller/store")]
[ApiController]
public class SellerStoreController(ISellerStoreService storeService) : ControllerBase
{
    private readonly ISellerStoreService _storeService = storeService;

    [HttpGet]
    [Authorize(Policy = PolicyNames.ActiveSeller)]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var result = await _storeService.GetMineAsync(User.GetUserId(), cancellationToken);
        return result.IsSucceed ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] UpsertStoreRequest request, CancellationToken cancellationToken)
    {
        var result = await _storeService.CreateAsync(User.GetUserId(), request, cancellationToken);
        return result.IsSucceed
            ? CreatedAtAction(nameof(GetMine), result.Value)
            : result.ToProblem();
    }

    [HttpPut]
    [Authorize(Policy = PolicyNames.ActiveSeller)]
    public async Task<IActionResult> Update([FromBody] UpsertStoreRequest request, CancellationToken cancellationToken)
    {
        var result = await _storeService.UpdateAsync(User.GetUserId(), request, cancellationToken);
        return result.IsSucceed ? Ok(result.Value) : result.ToProblem();
    }
}

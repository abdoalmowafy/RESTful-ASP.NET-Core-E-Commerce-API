using Catalog.Management.Contracts;
using Catalog.Management.Services;

namespace Catalog.Management.Controllers;

[Route("admin/promo-codes")]
[ApiController]
[Authorize]
public class PromoCodesController(IPromoCodeManagementService promoCodeService) : ControllerBase
{
    private readonly IPromoCodeManagementService _promoCodeService = promoCodeService;

    [HttpGet]
    [HasPermission(Permissions.PromoCodes.View)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await _promoCodeService.GetAsync(cancellationToken);
        return result.IsSucceed ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPost]
    [HasPermission(Permissions.PromoCodes.Create)]
    public async Task<IActionResult> Create([FromBody] PromoCodeRequest request, CancellationToken cancellationToken)
    {
        var result = await _promoCodeService.CreateAsync(request, cancellationToken);
        return result.IsSucceed ? CreatedAtAction(nameof(Get), result.Value) : result.ToProblem();
    }

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.PromoCodes.Update)]
    public async Task<IActionResult> Update(
        [FromRoute] int id,
        [FromBody] PromoCodeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _promoCodeService.UpdateAsync(id, request, cancellationToken);
        return result.IsSucceed ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPatch("{id:int}/status")]
    [HasPermission(Permissions.PromoCodes.Update)]
    public async Task<IActionResult> SetActive(
        [FromRoute] int id,
        [FromBody] StatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _promoCodeService.SetActiveAsync(id, request, cancellationToken);
        return result.IsSucceed ? NoContent() : result.ToProblem();
    }

    [HttpDelete("{id:int}")]
    [HasPermission(Permissions.PromoCodes.Delete)]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await _promoCodeService.DeleteAsync(id, User, cancellationToken);
        return result.IsSucceed ? NoContent() : result.ToProblem();
    }
}

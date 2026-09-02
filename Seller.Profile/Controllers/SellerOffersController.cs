using Seller.Profile.Contracts;
using Seller.Profile.Services;

namespace Seller.Profile.Controllers;

[Route("seller/offers")]
[ApiController]
[Authorize(Policy = PolicyNames.ActiveSeller)]
public class SellerOffersController(ISellerOfferService offerService) : ControllerBase
{
    private readonly ISellerOfferService _offerService = offerService;

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _offerService.GetAsync(User.GetUserId(), pageIndex, pageSize, cancellationToken);
        return result.IsSucceed ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] UpsertOfferRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _offerService.CreateAsync(User.GetUserId(), request, cancellationToken);
        return result.IsSucceed ? StatusCode(StatusCodes.Status201Created, result.Value) : result.ToProblem();
    }

    [HttpPut("{offerId:int}")]
    public async Task<IActionResult> Update(
        [FromRoute] int offerId,
        [FromBody] UpsertOfferRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _offerService.UpdateAsync(User.GetUserId(), offerId, request, cancellationToken);
        return result.IsSucceed ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPatch("{offerId:int}/active")]
    public async Task<IActionResult> SetActive(
        [FromRoute] int offerId,
        [FromBody] SetOfferActiveRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _offerService.SetActiveAsync(User.GetUserId(), offerId, request.IsActive, cancellationToken);
        return result.IsSucceed ? NoContent() : result.ToProblem();
    }

    [HttpDelete("{offerId:int}")]
    public async Task<IActionResult> Delete([FromRoute] int offerId, CancellationToken cancellationToken)
    {
        var result = await _offerService.DeleteAsync(User.GetUserId(), offerId, cancellationToken);
        return result.IsSucceed ? NoContent() : result.ToProblem();
    }
}

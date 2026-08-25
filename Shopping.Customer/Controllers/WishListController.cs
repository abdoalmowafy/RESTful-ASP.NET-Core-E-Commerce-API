using Shopping.Customer.Services;

namespace Shopping.Customer.Controllers;

[Route("wishlist")]
[ApiController]
[Authorize]
public class WishListController(IWishListService wishListService) : ControllerBase
{
    private readonly IWishListService _wishListService = wishListService;

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await _wishListService.GetAsync(User.GetUserId(), cancellationToken);
        return result.IsSucceed ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPost("{productId:int}")]
    public async Task<IActionResult> Add([FromRoute] int productId, CancellationToken cancellationToken)
    {
        var result = await _wishListService.AddAsync(User.GetUserId(), productId, cancellationToken);
        return result.IsSucceed ? Ok(result.Value) : result.ToProblem();
    }

    [HttpDelete("{productId:int}")]
    public async Task<IActionResult> Remove([FromRoute] int productId, CancellationToken cancellationToken)
    {
        var result = await _wishListService.RemoveAsync(User.GetUserId(), productId, cancellationToken);
        return result.IsSucceed ? NoContent() : result.ToProblem();
    }
}

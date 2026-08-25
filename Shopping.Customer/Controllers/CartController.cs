using Shopping.Customer.Contracts;
using Shopping.Customer.Services;

namespace Shopping.Customer.Controllers;

[Route("cart")]
[ApiController]
[Authorize]
public class CartController(ICartService cartService) : ControllerBase
{
    private readonly ICartService _cartService = cartService;

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
        => await WithCartAsync(_cartService.GetAsync(User.GetUserId(), cancellationToken));

    [HttpPost("items")]
    public async Task<IActionResult> AddItem([FromBody] AddCartItemRequest request, CancellationToken cancellationToken)
        => await WithCartAsync(_cartService.AddItemAsync(User.GetUserId(), request, cancellationToken));

    [HttpPut("items")]
    public async Task<IActionResult> UpdateItem([FromBody] UpdateCartItemRequest request, CancellationToken cancellationToken)
        => await WithCartAsync(_cartService.UpdateItemAsync(User.GetUserId(), request, cancellationToken));

    [HttpDelete("items/{productId:int}")]
    public async Task<IActionResult> RemoveItem([FromRoute] int productId, CancellationToken cancellationToken)
        => await WithCartAsync(_cartService.RemoveItemAsync(User.GetUserId(), productId, cancellationToken));

    [HttpDelete("items")]
    public async Task<IActionResult> Clear(CancellationToken cancellationToken)
        => await WithCartAsync(_cartService.ClearAsync(User.GetUserId(), cancellationToken));

    [HttpPost("promo-code")]
    public async Task<IActionResult> ApplyPromo([FromBody] ApplyPromoRequest request, CancellationToken cancellationToken)
        => await WithCartAsync(_cartService.ApplyPromoAsync(User.GetUserId(), request, cancellationToken));

    [HttpDelete("promo-code")]
    public async Task<IActionResult> RemovePromo(CancellationToken cancellationToken)
        => await WithCartAsync(_cartService.RemovePromoAsync(User.GetUserId(), cancellationToken));

    private async Task<IActionResult> WithCartAsync(Task<Result<CartResponse>> operation)
    {
        var result = await operation;
        return result.IsSucceed ? Ok(result.Value) : result.ToProblem();
    }
}

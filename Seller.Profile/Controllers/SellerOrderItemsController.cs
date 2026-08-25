using Seller.Profile.Contracts;
using Seller.Profile.Services;

namespace Seller.Profile.Controllers;

[Route("seller/order-items")]
[ApiController]
[Authorize(Policy = PolicyNames.ActiveSeller)]
public class SellerOrderItemsController(ISellerOrderService orderService) : ControllerBase
{
    private readonly ISellerOrderService _orderService = orderService;

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] OrderStatus? status,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _orderService.GetSoldItemsAsync(User.GetUserId(), status, pageIndex, pageSize, cancellationToken);
        return result.IsSucceed ? Ok(result.Value) : result.ToProblem();
    }
}

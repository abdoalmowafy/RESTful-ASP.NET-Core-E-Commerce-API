using Ordering.Customer.Contracts;
using Ordering.Customer.Services;

namespace Ordering.Customer.Controllers;

[Route("returns")]
[ApiController]
[Authorize]
public class ReturnsController(IReturnsService returnsService) : ControllerBase
{
    private readonly IReturnsService _returnsService = returnsService;

    [HttpGet]
    public async Task<IActionResult> GetMyReturns(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _returnsService.GetMyReturnsAsync(User.GetUserId(), pageIndex, pageSize, cancellationToken);
        return result.IsSucceed ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPost("order-products/{orderProductId:int}")]
    public async Task<IActionResult> Create(
        [FromRoute] int orderProductId,
        [FromBody] CreateReturnRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _returnsService.CreateAsync(User.GetUserId(), orderProductId, request, cancellationToken);
        return result.IsSucceed ? CreatedAtAction(nameof(GetMyReturns), result.Value) : result.ToProblem();
    }
}

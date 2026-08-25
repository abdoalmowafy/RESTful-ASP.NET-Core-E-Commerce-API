using Shopping.Customer.Contracts;
using Shopping.Customer.Services;

namespace Shopping.Customer.Controllers;

[Route("products/{productId:int}/reviews")]
[ApiController]
public class ReviewsController(IReviewService reviewService) : ControllerBase
{
    private readonly IReviewService _reviewService = reviewService;

    [HttpGet]
    public async Task<IActionResult> GetForProduct([FromRoute] int productId, CancellationToken cancellationToken)
    {
        var result = await _reviewService.GetForProductAsync(productId, cancellationToken);
        return result.IsSucceed ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(
        [FromRoute] int productId,
        [FromBody] ReviewRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _reviewService.CreateAsync(User.GetUserId(), productId, request, cancellationToken);
        return result.IsSucceed
            ? CreatedAtAction(nameof(GetForProduct), new { productId }, result.Value)
            : result.ToProblem();
    }

    [HttpPut("{reviewId:int}")]
    [Authorize]
    public async Task<IActionResult> Update(
        [FromRoute] int reviewId,
        [FromBody] ReviewRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _reviewService.UpdateAsync(User.GetUserId(), reviewId, request, User.IsStaff(), cancellationToken);
        return result.IsSucceed ? Ok(result.Value) : result.ToProblem();
    }

    [HttpDelete("{reviewId:int}")]
    [Authorize]
    public async Task<IActionResult> Delete([FromRoute] int reviewId, CancellationToken cancellationToken)
    {
        var result = await _reviewService.DeleteAsync(User.GetUserId(), reviewId, User.IsStaff(), cancellationToken);
        return result.IsSucceed ? NoContent() : result.ToProblem();
    }
}

using Driver.Profile.Services;

namespace Driver.Profile.Controllers;

[Route("driver/pickups")]
[ApiController]
[Authorize(Roles = DefaultRoles.Driver)]
public class PickupsController(IDeliveryService deliveryService) : ControllerBase
{
    private readonly IDeliveryService _deliveryService = deliveryService;

    [HttpGet]
    public async Task<IActionResult> GetMyPickups(CancellationToken cancellationToken)
    {
        var result = await _deliveryService.GetMyPickupsAsync(User.GetUserId(), cancellationToken);
        return result.IsSucceed ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPatch("{returnId:int}/collected")]
    public async Task<IActionResult> MarkCollected([FromRoute] int returnId, CancellationToken cancellationToken)
    {
        var result = await _deliveryService.MarkCollectedAsync(User.GetUserId(), returnId, cancellationToken);
        return result.IsSucceed ? NoContent() : result.ToProblem();
    }

    [HttpPatch("{returnId:int}/completed")]
    public async Task<IActionResult> MarkCompleted([FromRoute] int returnId, CancellationToken cancellationToken)
    {
        var result = await _deliveryService.MarkCompletedAsync(User.GetUserId(), returnId, cancellationToken);
        return result.IsSucceed ? NoContent() : result.ToProblem();
    }
}

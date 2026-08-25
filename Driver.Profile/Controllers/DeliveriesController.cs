using Driver.Profile.Services;

namespace Driver.Profile.Controllers;

[Route("driver/deliveries")]
[ApiController]
[Authorize(Roles = DefaultRoles.Driver)]
public class DeliveriesController(IDeliveryService deliveryService) : ControllerBase
{
    private readonly IDeliveryService _deliveryService = deliveryService;

    [HttpGet]
    public async Task<IActionResult> GetMyDeliveries(CancellationToken cancellationToken)
    {
        var result = await _deliveryService.GetMyDeliveriesAsync(User.GetUserId(), cancellationToken);
        return result.IsSucceed ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPatch("{orderId:int}/picked-up")]
    public async Task<IActionResult> MarkPickedUp([FromRoute] int orderId, CancellationToken cancellationToken)
    {
        var result = await _deliveryService.MarkPickedUpAsync(User.GetUserId(), orderId, cancellationToken);
        return result.IsSucceed ? NoContent() : result.ToProblem();
    }

    [HttpPatch("{orderId:int}/delivered")]
    public async Task<IActionResult> MarkDelivered([FromRoute] int orderId, CancellationToken cancellationToken)
    {
        var result = await _deliveryService.MarkDeliveredAsync(User.GetUserId(), orderId, cancellationToken);
        return result.IsSucceed ? NoContent() : result.ToProblem();
    }
}

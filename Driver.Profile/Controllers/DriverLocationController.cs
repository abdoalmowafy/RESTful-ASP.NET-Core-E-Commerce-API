using Driver.Profile.Contracts;

namespace Driver.Profile.Controllers;

[Route("driver/orders")]
[ApiController]
[Authorize(Policy = PolicyNames.ActiveDriver)]
public class DriverLocationController(IDriverLocationService locationService) : ControllerBase
{
    private readonly IDriverLocationService _locationService = locationService;

    [HttpPost("{orderId:int}/location")]
    public async Task<IActionResult> Ping(
        [FromRoute] int orderId,
        [FromBody] UpdateDriverLocationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _locationService.PingAsync(User.GetUserId(), orderId, request.Latitude, request.Longitude, cancellationToken);
        return result.IsSucceed ? NoContent() : result.ToProblem();
    }
}

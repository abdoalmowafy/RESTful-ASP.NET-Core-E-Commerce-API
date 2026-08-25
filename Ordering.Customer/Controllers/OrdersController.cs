using Ordering.Customer.Contracts;
using Ordering.Customer.Services;

namespace Ordering.Customer.Controllers;

[Route("orders")]
[ApiController]
[Authorize]
public class OrdersController(IOrdersService ordersService, IDriverLocationService locationService) : ControllerBase
{
    private readonly IOrdersService _ordersService = ordersService;
    private readonly IDriverLocationService _locationService = locationService;

    [HttpGet]
    public async Task<IActionResult> GetMyOrders(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _ordersService.GetMyOrdersAsync(User.GetUserId(), pageIndex, pageSize, cancellationToken);
        return result.IsSucceed ? Ok(result.Value) : result.ToProblem();
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await _ordersService.GetAsync(User.GetUserId(), id, User.IsStaff(), cancellationToken);
        return result.IsSucceed ? Ok(result.Value) : result.ToProblem();
    }

    [HttpGet("{id:int}/timeline")]
    public async Task<IActionResult> GetTimeline([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await _ordersService.GetTimelineAsync(User.GetUserId(), id, User.IsStaff(), cancellationToken);
        return result.IsSucceed ? Ok(result.Value) : result.ToProblem();
    }

    [HttpGet("{id:int}/driver-location")]
    public async Task<IActionResult> GetDriverLocation([FromRoute] int id, CancellationToken cancellationToken)
    {
        var access = await _ordersService.GetAsync(User.GetUserId(), id, User.IsStaff(), cancellationToken);
        if (access.IsFailure)
            return access.ToProblem();

        var result = await _locationService.GetLatestAsync(id, cancellationToken);
        return result.IsSucceed
            ? Ok(new { result.Value.Point.Latitude, result.Value.Point.Longitude, result.Value.Point.RecordedAt, result.Value.EtaMinutes })
            : result.ToProblem();
    }

    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request, CancellationToken cancellationToken)
    {
        var result = await _ordersService.CheckoutAsync(User.GetUserId(), request, cancellationToken);
        return result.IsSucceed ? Accepted(result.Value) : result.ToProblem();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Cancel([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await _ordersService.CancelAsync(User.GetUserId(), id, User, cancellationToken);
        return result.IsSucceed ? NoContent() : result.ToProblem();
    }
}

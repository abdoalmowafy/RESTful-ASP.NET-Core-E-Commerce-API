using Ordering.Customer.Contracts;
using Ordering.Customer.Services;

namespace Ordering.Customer.Controllers;

[Route("payments/paymob")]
[ApiController]
public class PaymobController(IPaymobCallbackService callbackService) : ControllerBase
{
    private readonly IPaymobCallbackService _callbackService = callbackService;

    [HttpPost("callback")]
    public async Task<IActionResult> Callback(
        [FromQuery] string? hmac,
        [FromBody] PaymobCallbackPayload payload,
        CancellationToken cancellationToken)
    {
        var result = await _callbackService.HandleAsync(hmac, payload, cancellationToken);
        return result.IsSucceed ? Ok(new { Message = "Callback processed" }) : result.ToProblem();
    }
}

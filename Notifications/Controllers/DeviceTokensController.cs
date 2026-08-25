using Notifications.Contracts;
using Notifications.Services;

namespace Notifications.Controllers;

[Route("notifications/device-tokens")]
[ApiController]
[Authorize]
public class DeviceTokensController(IDeviceRegistryService registryService) : ControllerBase
{
    private readonly IDeviceRegistryService _registryService = registryService;

    [HttpPost]
    public async Task<IActionResult> Register([FromBody] RegisterDeviceRequest request, CancellationToken cancellationToken)
    {
        var result = await _registryService.RegisterAsync(User, request.Token, request.Platform, request.DeviceName, cancellationToken);
        return result.IsSucceed ? NoContent() : result.ToProblem();
    }

    [HttpGet]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var result = await _registryService.GetMineAsync(User, cancellationToken);
        return result.IsSucceed ? Ok(result.Value) : result.ToProblem();
    }

    [HttpDelete]
    public async Task<IActionResult> Unregister([FromQuery] string token, CancellationToken cancellationToken)
    {
        var result = await _registryService.UnregisterAsync(User, token, cancellationToken);
        return result.IsSucceed ? NoContent() : result.ToProblem();
    }
}

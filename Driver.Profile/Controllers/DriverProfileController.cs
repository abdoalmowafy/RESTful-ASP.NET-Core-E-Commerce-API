using Driver.Profile.Contracts;
using Driver.Profile.Services;

namespace Driver.Profile.Controllers;

[Route("driver/profile")]
[ApiController]
public class DriverProfileController(IDriverProfileService profileService) : ControllerBase
{
    private readonly IDriverProfileService _profileService = profileService;

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var result = await _profileService.GetMineAsync(User.GetUserId(), cancellationToken);
        return result.IsSucceed ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Apply([FromBody] ApplyDriverRequest request, CancellationToken cancellationToken)
    {
        var result = await _profileService.ApplyAsync(User.GetUserId(), request, cancellationToken);
        return result.IsSucceed ? CreatedAtAction(nameof(GetMine), result.Value) : result.ToProblem();
    }

    [HttpPut]
    [Authorize(Policy = PolicyNames.PendingDriver)]
    public async Task<IActionResult> Resubmit([FromBody] ApplyDriverRequest request, CancellationToken cancellationToken)
    {
        var result = await _profileService.ResubmitAsync(User.GetUserId(), request, cancellationToken);
        return result.IsSucceed ? Ok(result.Value) : result.ToProblem();
    }
}

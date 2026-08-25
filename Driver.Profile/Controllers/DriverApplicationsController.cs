using Driver.Profile.Contracts;
using Driver.Profile.Services;

namespace Driver.Profile.Controllers;

[Route("driver/requests")]
[ApiController]
[Authorize]
public class DriverApplicationsController(IDriverApplicationService applicationService) : ControllerBase
{
    private readonly IDriverApplicationService _applicationService = applicationService;

    [HttpPost("apply")]
    public async Task<IActionResult> Apply(
        [FromForm] ApplyDriverForm form,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _applicationService.ApplyAsync(User.GetUserId(), form, cancellationToken);
            return result.IsSucceed
                ? CreatedAtAction(nameof(Apply), result.Value)
                : result.ToProblem();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(Error.BadRequest("Driver.InvalidDocument", ex.Message)).ToProblem();
        }
    }

    [HttpPut("resubmit")]
    [Authorize(Policy = PolicyNames.PendingDriver)]
    public async Task<IActionResult> Resubmit(
        [FromForm] ApplyDriverForm form,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _applicationService.ResubmitAsync(User.GetUserId(), form, cancellationToken);
            return result.IsSucceed ? Ok(result.Value) : result.ToProblem();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(Error.BadRequest("Driver.InvalidDocument", ex.Message)).ToProblem();
        }
    }
}

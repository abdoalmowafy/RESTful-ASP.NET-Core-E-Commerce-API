using Admin.Profile.Contracts;
using Admin.Profile.Services;

namespace Admin.Profile.Controllers;

[Route("admin/profile")]
[ApiController]
[Authorize]
public class AdminProfileController(IAdminProfileService profileService) : ControllerBase
{
    private readonly IAdminProfileService _profileService = profileService;

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await _profileService.GetAsync(User, cancellationToken);
        return result.IsSucceed ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateAdminProfileRequest request, CancellationToken cancellationToken)
    {
        var result = await _profileService.UpdateAsync(User, request, cancellationToken);
        return result.IsSucceed ? Ok(result.Value) : result.ToProblem();
    }
}

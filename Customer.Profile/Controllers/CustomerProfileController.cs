using Customer.Profile.Contracts;
using Customer.Profile.Services;

namespace Customer.Profile.Controllers;

[Route("customer/profile")]
[ApiController]
[Authorize]
public class CustomerProfileController(ICustomerProfileService profileService) : ControllerBase
{
    private readonly ICustomerProfileService _profileService = profileService;

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await _profileService.GetAsync(User, cancellationToken);
        return result.IsSucceed ? Ok(result.Value) : result.ToProblem();
    }
}

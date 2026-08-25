using Customer.Profile.Contracts;
using Customer.Profile.Services;

namespace Customer.Profile.Controllers;

[Route("addresses")]
[ApiController]
[Authorize]
public class AddressesController(IAddressService addressService) : ControllerBase
{
    private readonly IAddressService _addressService = addressService;

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await _addressService.GetAsync(User.GetUserId(), cancellationToken);
        return result.IsSucceed ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAddressRequest request, CancellationToken cancellationToken)
    {
        var result = await _addressService.CreateAsync(User.GetUserId(), request, cancellationToken);
        return result.IsSucceed ? CreatedAtAction(nameof(Get), result.Value) : result.ToProblem();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await _addressService.DeleteAsync(User.GetUserId(), id, cancellationToken);
        return result.IsSucceed ? NoContent() : result.ToProblem();
    }
}

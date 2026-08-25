using Customer.Management.Contracts;
using Customer.Management.Services;

namespace Customer.Management.Controllers;

[Route("admin/customers")]
[ApiController]
[Authorize]
public class CustomersController(ICustomerManagementService customerService) : ControllerBase
{
    private readonly ICustomerManagementService _customerService = customerService;

    [HttpGet]
    [HasPermission(Permissions.Customers.View)]
    public async Task<IActionResult> Get(
        [FromQuery] string? search,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _customerService.GetAsync(search, pageIndex, pageSize, cancellationToken);
        return result.IsSucceed ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPatch("{customerId}/status")]
    [HasPermission(Permissions.Customers.Manage)]
    public async Task<IActionResult> UpdateStatus(
        [FromRoute] string customerId,
        [FromBody] UpdateCustomerStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _customerService.UpdateStatusAsync(customerId, request, cancellationToken);
        return result.IsSucceed ? NoContent() : result.ToProblem();
    }
}

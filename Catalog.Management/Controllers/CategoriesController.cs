using Catalog.Management.Contracts;
using Catalog.Management.Services;

namespace Catalog.Management.Controllers;

[Route("admin/categories")]
[ApiController]
[Authorize]
public class CategoriesController(ICategoryManagementService categoryService) : ControllerBase
{
    private readonly ICategoryManagementService _categoryService = categoryService;

    [HttpGet]
    [HasPermission(Permissions.Products.View)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await _categoryService.GetAsync(cancellationToken);
        return result.IsSucceed ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPost]
    [HasPermission(Permissions.Categories.Manage)]
    public async Task<IActionResult> Create([FromBody] CategoryRequest request, CancellationToken cancellationToken)
    {
        var result = await _categoryService.CreateAsync(request, cancellationToken);
        return result.IsSucceed ? CreatedAtAction(nameof(Get), result.Value) : result.ToProblem();
    }

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.Categories.Manage)]
    public async Task<IActionResult> Update(
        [FromRoute] int id,
        [FromBody] CategoryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _categoryService.UpdateAsync(id, request, cancellationToken);
        return result.IsSucceed ? Ok(result.Value) : result.ToProblem();
    }

    [HttpDelete("{id:int}")]
    [HasPermission(Permissions.Categories.Manage)]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await _categoryService.DeleteAsync(id, User, cancellationToken);
        return result.IsSucceed ? NoContent() : result.ToProblem();
    }
}

using Catalog.Management.Contracts;
using Catalog.Management.Services;
using Microsoft.AspNetCore.Authorization;

namespace Catalog.Management.Controllers;

[Route("admin/products")]
[ApiController]
[Authorize]
public class ProductsController(IProductManagementService productService) : ControllerBase
{
    private readonly IProductManagementService _productService = productService;

    [HttpGet]
    [HasPermission(Permissions.Products.View)]
    public async Task<IActionResult> Get(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _productService.GetAsync(pageIndex, pageSize, includeDeleted, cancellationToken);
        return result.IsSucceed ? Ok(result.Value) : result.ToProblem();
    }

    [HttpGet("{id:int}")]
    [HasPermission(Permissions.Products.View)]
    public async Task<IActionResult> Get([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await _productService.GetAsync(id, cancellationToken);
        return result.IsSucceed ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPost]
    [HasPermission(Permissions.Products.Create)]
    public async Task<IActionResult> Create(
        [FromForm] ProductRequest request,
        IList<IFormFile> media,
        CancellationToken cancellationToken)
    {
        var result = await _productService.CreateAsync(request, media ?? [], cancellationToken);
        return result.IsSucceed
            ? CreatedAtAction(nameof(Get), new { id = result.Value.Id }, result.Value)
            : result.ToProblem();
    }

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.Products.Update)]
    public async Task<IActionResult> Update(
        [FromRoute] int id,
        [FromBody] ProductRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _productService.UpdateAsync(id, request, cancellationToken);
        return result.IsSucceed ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPatch("{id:int}/stock")]
    [HasPermission(Permissions.Products.Update)]
    public async Task<IActionResult> SetStock(
        [FromRoute] int id,
        [FromBody] StockRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _productService.SetStockAsync(id, request, cancellationToken);
        return result.IsSucceed ? NoContent() : result.ToProblem();
    }

    [HttpDelete("{id:int}")]
    [HasPermission(Permissions.Products.Delete)]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await _productService.DeleteAsync(id, User, cancellationToken);
        return result.IsSucceed ? NoContent() : result.ToProblem();
    }
}

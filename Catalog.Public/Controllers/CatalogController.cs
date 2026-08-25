using Catalog.Public.Contracts;
using Catalog.Public.Services;
using Microsoft.AspNetCore.Authorization;

namespace Catalog.Public.Controllers;

[Route("store")]
[ApiController]
public class CatalogController(ICatalogService catalogService) : ControllerBase
{
    private readonly ICatalogService _catalogService = catalogService;

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
    {
        var result = await _catalogService.GetCategoriesAsync(cancellationToken);
        return result.IsSucceed ? Ok(result.Value) : result.ToProblem();
    }

    [HttpGet("home")]
    public async Task<IActionResult> GetHome(CancellationToken cancellationToken)
    {
        var result = await _catalogService.GetHomeAsync(cancellationToken);
        return result.IsSucceed ? Ok(result.Value) : result.ToProblem();
    }

    [HttpGet("products/{productId:int}")]
    public async Task<IActionResult> GetProduct([FromRoute] int productId, CancellationToken cancellationToken)
    {
        var result = await _catalogService.GetProductAsync(productId, cancellationToken);
        return result.IsSucceed ? Ok(result.Value) : result.ToProblem();
    }

    [HttpGet("search/{categoryName}/{keyWord?}")]
    public async Task<IActionResult> Search(
        [FromRoute] string categoryName,
        [FromRoute] string? keyWord,
        [FromQuery] bool includeOutOfStock = false,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var request = new SearchRequest(
            keyWord ?? string.Empty,
            categoryName,
            includeOutOfStock,
            pageIndex,
            Math.Clamp(pageSize, 1, 50));

        var result = await _catalogService.SearchAsync(request, User, cancellationToken);
        return result.IsSucceed ? Ok(result.Value) : result.ToProblem();
    }
}

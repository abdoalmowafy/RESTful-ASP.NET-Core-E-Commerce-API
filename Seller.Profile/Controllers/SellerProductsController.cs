using Seller.Profile.Contracts;
using Seller.Profile.Services;

namespace Seller.Profile.Controllers;

[Route("seller/products")]
[ApiController]
[Authorize]
public class SellerProductsController(ISellerProductService productService) : ControllerBase
{
    private readonly ISellerProductService _productService = productService;

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _productService.GetAsync(User.GetUserId(), pageIndex, pageSize, cancellationToken);
        return result.IsSucceed ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromForm] SellerProductRequest request,
        IList<IFormFile> media,
        CancellationToken cancellationToken)
    {
        var result = await _productService.CreateAsync(User.GetUserId(), request, media ?? [], cancellationToken);
        return result.IsSucceed ? StatusCode(StatusCodes.Status201Created, result.Value) : result.ToProblem();
    }

    [HttpPut("{productId:int}")]
    public async Task<IActionResult> Update(
        [FromRoute] int productId,
        [FromBody] SellerProductRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _productService.UpdateAsync(User.GetUserId(), productId, request, cancellationToken);
        return result.IsSucceed ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPatch("{productId:int}/stock")]
    public async Task<IActionResult> SetStock(
        [FromRoute] int productId,
        [FromBody] SellerStockRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _productService.SetStockAsync(User.GetUserId(), productId, request, cancellationToken);
        return result.IsSucceed ? NoContent() : result.ToProblem();
    }

    [HttpDelete("{productId:int}")]
    public async Task<IActionResult> Delete([FromRoute] int productId, CancellationToken cancellationToken)
    {
        var result = await _productService.DeleteAsync(User.GetUserId(), productId, User, cancellationToken);
        return result.IsSucceed ? NoContent() : result.ToProblem();
    }
}

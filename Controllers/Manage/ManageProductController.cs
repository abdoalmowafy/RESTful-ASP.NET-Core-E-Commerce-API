using ECommerceAPI.Data;
using ECommerceAPI.DTOs.Requests;
using ECommerceAPI.Models;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceAPI.Controllers.Manage
{
    [Route("api/manage/[action]")]
    [ApiController]
    [Authorize(Roles = "Admin,Moderator")]
    public class ManageProductController(DataContext context) : ControllerBase
    {
        private readonly DataContext _context = context;

        [HttpGet]
        public async Task<IActionResult> IndexProducts()
        {
            var products = await _context.Products.AsNoTracking().ToListAsync();
            return Ok(products);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> IndexProduct(int id)
        {
            var product = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);

            if (product is null)
                return NotFound("Product was not found!");

            string path = Path.Combine("wwwroot/Media/ProductMedia/", product.Id.ToString());
            string[] fileNames = Directory.GetFiles(path);
            List<string> fileNamesOnly = fileNames.Select(filePath => Path.GetFileName(filePath)).ToList();

            return Ok(new { product, mediaFiles = fileNamesOnly });
        }

        [HttpPost]
        public async Task<IActionResult> NewProduct([FromBody] ProductRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var product = request.Adapt<Product>();
            product.CategoryId = request.Category.Id;

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            string directoryPath = Path.Combine("wwwroot/Media/ProductMedia/", product.Id.ToString());
            Directory.CreateDirectory(directoryPath);

            foreach (var file in product.Media)
            {
                string filePath = Path.Combine(directoryPath, file.FileName);
                using var fileStream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(fileStream);
            }

            return Ok(product);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<Product>> EditProduct(int id, [FromBody] ProductRequest request)
        {
            var product = await _context.Products.FindAsync(id);

            if (product is null)
                return NotFound("Product was not found!");

            if (!ModelState.IsValid) return BadRequest(ModelState);

            request.Adapt(product);

            if (product.Media is not null && product.Media.Count != 0)
            {
                string directoryPath = Path.Combine("wwwroot/Media/ProductMedia/", product.Id.ToString());
                Directory.CreateDirectory(directoryPath);

                foreach (var file in product.Media)
                {
                    string filePath = Path.Combine(directoryPath, file.FileName);
                    using var fileStream = new FileStream(filePath, FileMode.Create);
                    await file.CopyToAsync(fileStream);
                }
            }

            _context.Products.Update(product);
            await _context.SaveChangesAsync(_context.Users.AsNoTracking().First(u => u.UserName == User.Identity!.Name));

            return Ok(request);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product is null)
                return NotFound("Product was not found!");

            product.DeletedDateTime = DateTime.Now;
            _context.Products.Update(product);
            _context.DeletesHistory.Add(new DeleteHistory
            {
                Deleter = await _context.Users.AsNoTracking().FirstAsync(u => u.UserName == User.Identity!.Name),
                DeletedType = nameof(Product),
                DeletedId = id
            });

            var inWishlist = await _context.Users
                .Include(u => u.WishList)
                .Where(u => u.WishList.Any(p => p.Id == id))
                .ToListAsync();

            foreach (var user in inWishlist)
            {
                user.WishList.Remove(product);
                _context.Users.Update(user);
            }

            var inCart = await _context.Users
                .Include(u => u.Cart)
                    .ThenInclude(c => c.CartProducts)
                        .ThenInclude(cp => cp.Product)
                .Where(u => u.Cart.CartProducts.Any(cp => cp.Product.Id == id))
                .ToListAsync();

            foreach (var user in inCart)
            {
                var cartProduct = user.Cart.CartProducts.First(cp => cp.Product.Id == id);
                user.Cart.CartProducts.Remove(cartProduct);
                _context.Users.Update(user);
            }

            await _context.SaveChangesAsync();

            return Ok("Product deleted successfully!");
        }

        [HttpPost("{id}")]
        public async Task<IActionResult> RecoverProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product is null) return NotFound("Product was not found!");

            product.DeletedDateTime = null;
            _context.Products.Update(product);
            await _context.SaveChangesAsync(_context.Users.AsNoTracking().First(u => u.UserName == User.Identity!.Name));

            return Ok(product);
        }
    }
}

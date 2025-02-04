using ECommerceAPI.Data;
using ECommerceAPI.DTOs.Responses;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceAPI.Controllers
{
    [Route("api/[action]")]
    [ApiController]
    [Authorize]
    public class WishlistController(DataContext context) : ControllerBase
    {
        private readonly DataContext _context = context;

        [HttpGet]
        public async Task<IActionResult> IndexWishlist(int pageIndex = 1)
        {
            var response = await _context.Users
                .Include(u => u.WishList)
                    .ThenInclude(p => p.Category)
                .AsNoTracking()
                .Where(u => u.UserName == User.Identity!.Name)
                .SelectMany(user => user.WishList)
                .Select(p => p.Adapt<ProductPriefResponse>())
                .ToPaginatedListAsync(pageIndex, 20);

            return Ok(response);
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> ModifyWishlist(int id)
        {
            var user = await _context.Users
                .Include(u => u.WishList)
                .FirstAsync(u => u.UserName == User.Identity!.Name);

            var product = await _context.Products.FindAsync(id);

            if (product is null || product.DeletedDateTime is not null) return NotFound("Product not found!");

            if (!user.WishList.Remove(product))
            {
                user.WishList.Add(product);
            }

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Ok(product.Adapt<ProductPriefResponse>());
        }
    }
}

using ECommerceAPI.Data;
using ECommerceAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceAPI.Controllers.Manage
{
    [Route("api/manage/Category")]
    [ApiController]
    [Authorize(Roles = "Admin,Moderator")]
    public class ManageCategoryController(DataContext context) : ControllerBase
    {
        private readonly DataContext _context = context;

        [HttpGet]
        public async Task<IActionResult> IndexCategories()
        {
            var categories = await _context.Categories.Include(c => c.Products).AsNoTracking().ToListAsync();
            return Ok(categories);
        }

        [HttpPost]
        public async Task<IActionResult> NewCategory([FromForm] string categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
                return BadRequest("Category name cannot be empty!");

            if (await _context.Categories.AsNoTracking().AnyAsync(c => c.Name.Contains(categoryName)))
                return BadRequest("Category name already exists!");

            var category = new Category { Name = categoryName };
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return Ok(category);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.Id == id);

            if (category is null)
                return NotFound("Category not found!");

            if (category.Products.Count > 0)
                return BadRequest("Cannot delete category with one or more products!");

            _context.Categories.Remove(category);
            _context.DeletesHistory.Add(new DeleteHistory
            {
                Deleter = await _context.Users.AsNoTracking().FirstAsync(u => u.UserName == User.Identity!.Name),
                DeletedType = nameof(Category),
                DeletedId = id
            });
            await _context.SaveChangesAsync();

            return Ok("Category deleted successfully!");
        }
    }
}

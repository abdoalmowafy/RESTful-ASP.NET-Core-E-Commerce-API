using ECommerceAPI.Data;
using ECommerceAPI.DTOs.Requests;
using ECommerceAPI.DTOs.Responses;
using ECommerceAPI.Models;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceAPI.Controllers
{
    [Route("api/[action]")]
    [ApiController]
    [Authorize]
    public class ReviewController(DataContext context) : ControllerBase
    {
        private readonly DataContext _context = context;

        [HttpPost]
        public async Task<IActionResult> AddReview([FromBody] ReviewRequest request)
        {
            var user = await _context.Users
                .Include(u => u.Orders)
                    .ThenInclude(o => o.OrderProducts)
                        .ThenInclude(op => op.Product)
                .FirstAsync(u => u.UserName == User.Identity!.Name);

            var product = await _context.Products
                .Include(p => p.Reviews)
                .FirstOrDefaultAsync(p => p.Id == request.ProductId);

            if (product is null) return NotFound("Product not found!");

            if (product.Reviews.Any(r => r.Reviewer == user && r.DeletedDateTime is null))
                return BadRequest("You already reviewed this product!");

            if (!user.Orders.Where(o => o.Status == OrderStatus.Delivered)
                .SelectMany(o => o.OrderProducts).Any(op => op.Product.Id == product.Id))
                return BadRequest("You can't review a product you didn't buy!");

            if (!ModelState.IsValid) return BadRequest(ModelState);

            var review = request.Adapt<Review>();
            review.Reviewer = user;
            review.Product = product;
            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            return Ok(review.Adapt<ReviewResponse>());
        }

        [HttpPut]
        public async Task<IActionResult> EditReview([FromBody] ReviewRequest request)
        {
            var user = await _context.Users.FirstAsync(u => u.UserName == User.Identity!.Name);

            var review = await _context.Reviews
                .Include(r => r.Reviewer)
                .FirstOrDefaultAsync(r => r.Reviewer == user && r.ProductId == request.ProductId);

            if (review is null || review.DeletedDateTime.HasValue) return NotFound("Review not found!");

            if (review.Reviewer != user) return Forbid();

            if (!ModelState.IsValid) return BadRequest(ModelState);

            review.Rating = request.Rating;
            review.Text = request.Text;
            _context.Reviews.Update(review);
            await _context.SaveChangesAsync(user);

            return Ok(review.Adapt<ReviewResponse>());
        }

        [HttpDelete("id")]
        public async Task<IActionResult> DeleteReview(int id)
        {
            var review = await _context.Reviews
               .Include(r => r.Reviewer)
               .FirstOrDefaultAsync(r => r.Id == id);

            if (review is null || review.DeletedDateTime.HasValue) return NotFound("Review not found!");

            var user = await _context.Users.FirstAsync(u => u.UserName == User.Identity!.Name);

            if (review.Reviewer != user && !User.IsInRole("Admin") && !User.IsInRole("Moderator")) return Forbid();

            review.DeletedDateTime = DateTime.Now;
            _context.Reviews.Update(review);
            _context.DeletesHistory.Add(new DeleteHistory
            {
                Deleter = user,
                DeletedType = nameof(Review),
                DeletedId = review.Id
            });
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}

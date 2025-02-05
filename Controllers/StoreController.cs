using ECommerceAPI.Data;
using ECommerceAPI.DTOs.Responses;
using ECommerceAPI.Models;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceAPI.Controllers
{
    [Route("api")]
    [ApiController]
    public class StoreController(IDbContextFactory<DataContext> contextFactory, DataContext context) : ControllerBase
    {
        private readonly IDbContextFactory<DataContext> _contextFactory = contextFactory;
        private readonly DataContext _context = context;

        [HttpGet]
        public IActionResult GetCategoriesNames() => Ok(_context.Categories.AsNoTracking().Select(c => c.Name));

        [HttpGet]
        public async Task<IActionResult> Home()
        {
            using var context1 = await _contextFactory.CreateDbContextAsync();
            using var context2 = await _contextFactory.CreateDbContextAsync();
            using var context3 = await _contextFactory.CreateDbContextAsync();

            var orderedByOrdersTask = context1.Products
                .Include(p => p.Category)
                .AsNoTracking()
                .Where(p => p.DeletedDateTime == null && p.Quantity > 0)
                .GroupJoin(context1.OrderProducts.AsNoTracking(), p => p.Id, op => op.Product.Id,
                    (product, orderGroup) => new
                    {
                        Product = product,
                        TotalOrders = orderGroup.Sum(op => op.Quantity)
                    })
                .OrderByDescending(p => p.TotalOrders)
                .Take(25)
                .Select(p => p.Product.Adapt<ProductPriefResponse>())
                .ToListAsync();

            var orderedBySaleTask = context2.Products
                .Include(p => p.Category)
                .AsNoTracking()
                .Where(p => p.DeletedDateTime == null && p.Quantity > 0)
                .OrderByDescending(p => p.SalePercent)
                .Take(25)
                .Select(p => p.Adapt<ProductPriefResponse>())
                .ToListAsync();

            var orderedByCreatedTimeTask = context3.Products
                .Include(p => p.Category)
                .AsNoTracking()
                .Where(p => p.DeletedDateTime == null && p.Quantity > 0)
                .OrderByDescending(p => p.CreatedDateTime)
                .Take(25)
                .Select(p => p.Adapt<ProductPriefResponse>())
                .ToListAsync();

            var results = await Task.WhenAll(
                orderedByOrdersTask,
                orderedBySaleTask,
                orderedByCreatedTimeTask
            );

            return Ok(new
            {
                orderedByOrders = results[0],
                orderedBySale = results[1],
                orderedByCreatedTime = results[2]
            });
        }

        [HttpGet("{categoryName}/{keyWord}")]
        public async Task<IActionResult> Search(string keyWord, string categoryName = "All", bool includeOutOfStock = false, bool includeDeleted = false, int pageIndex = 1)
        {
            var category = await _context.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Name == categoryName);

            if (categoryName != "All" && category is null) return BadRequest("Invalid category name!");

            var products = _context.Products.Include(p => p.Category).AsNoTracking();

            if (!string.IsNullOrEmpty(keyWord))
            {
                products = products.Where(p => p.Name.Contains(keyWord) || p.Description.Contains(keyWord));
            }

            if (category is not null)
            {
                products = products.Where(p => p.Category!.Id == category.Id);
            }

            if (!includeDeleted)
            {
                products = products.Where(p => p.DeletedDateTime == null);
            }
            else if (!User.IsInRole("Admin") && !User.IsInRole("Moderator"))
            {
                return Forbid();
            }

            if (!includeOutOfStock)
            {
                products = products.Where(p => p.Quantity > 0);
            }

            if (!string.IsNullOrEmpty(keyWord))
            {
                _context.Searches.Add(new Search
                {
                    User = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserName == User.Identity!.Name),
                    Category = category,
                    KeyWord = keyWord
                });
                await _context.SaveChangesAsync();
            }

            return Ok(new
            {
                products = await products.Select(p => p.Adapt<ProductPriefResponse>()).ToPaginatedListAsync(pageIndex, 20),
                keyWord,
                categoryName,
                includeOutOfStock,
                includeDeleted
            });
        }

        [HttpGet("{id}"), ActionName("product")]
        public async Task<IActionResult> FullView(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Reviews)
                    .ThenInclude(r => r.Reviewer)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product is null) return NotFound("Product not found!");

            product.Views++;
            _context.Products.Update(product);
            await _context.SaveChangesAsync(_context.Users.AsNoTracking().FirstOrDefault(u => u.UserName == User.Identity!.Name));

            string path = Path.Combine("wwwroot\\Media\\ProductMedia\\", product.Id.ToString());
            string[] fileNames = Directory.GetFiles(path);
            List<string> fileNamesOnly = fileNames.Select(filePath => Path.GetFileName(filePath)).ToList();

            var inCart = false;
            var inWishlist = false;
            var reviewable = false;

            if (User.Identity!.IsAuthenticated)
            {
                var user = await _context.Users
                    .Include(u => u.Cart)
                        .ThenInclude(c => c.CartProducts)
                    .Include(u => u.Orders)
                        .ThenInclude(o => o.OrderProducts)
                            .ThenInclude(op => op.Product)
                    .Include(u => u.WishList)
                    .AsNoTracking()
                    .FirstAsync(u => u.UserName == User.Identity.Name);

                var cart = user.Cart!;
                var wishList = user.WishList;

                if (cart.CartProducts is not null && cart.CartProducts.Any(cp => cp.Product == product))
                    inCart = true;

                if (user.WishList is not null && wishList.Contains(product))
                    inWishlist = true;

                if (!product.Reviews.Any(rev => rev.Reviewer == user && rev.DeletedDateTime is null)
                    && user.Orders.SelectMany(o => o.OrderProducts).Any(op => op.Product == product))
                    reviewable = true;
            }

            return Ok(new
            {
                product = product.Adapt<ProductDetailedResponse>(),
                inCart,
                inWishlist,
                reviewable,
                MediaFiles = fileNamesOnly
            });
        }
    }
}
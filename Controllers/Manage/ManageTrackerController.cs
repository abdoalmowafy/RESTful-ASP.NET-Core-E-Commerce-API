using ECommerceAPI.Data;
using ECommerceAPI.DTOs.Responses;
using ECommerceAPI.Models;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceAPI.Controllers.Manage
{
    [Route("api/manage/[action]")]
    [ApiController]
    [Authorize(Roles = "Admin,Moderator")]
    public class ManageTrackerController(DataContext context, UserManager<User> userManager) : ControllerBase
    {
        private readonly DataContext _context = context;
        private readonly UserManager<User> _userManager = userManager;

        [HttpGet]
        public async Task<IActionResult> EditHistory(
            string? editorUsername,
            string? editedType,
            string? editedId,
            string? editedField,
            string? oldData,
            string? newData,
            DateTime? startDate,
            DateTime? endDate,
            int pageIndex = 1)
        {
            var edits = _context.EditHistories.Include(eh => eh.Editor).AsNoTracking();

            if (!string.IsNullOrWhiteSpace(editorUsername)) edits = edits.Where(e => e.Editor != null && e.Editor.UserName!.Contains(editorUsername));
            if (!string.IsNullOrWhiteSpace(editedType)) edits = edits.Where(e => e.EditedType.Contains(editedType));
            if (!string.IsNullOrWhiteSpace(editedId)) edits = edits.Where(e => e.EditedId.Contains(editedId));
            if (!string.IsNullOrWhiteSpace(editedField)) edits = edits.Where(e => e.EditedField.Contains(editedField));
            if (!string.IsNullOrWhiteSpace(oldData)) edits = edits.Where(e => e.OldData.Contains(oldData));
            if (!string.IsNullOrWhiteSpace(newData)) edits = edits.Where(e => e.NewData.Contains(newData));
            if (startDate is not null) edits = edits.Where(e => e.EditDateTime > startDate);
            if (endDate is not null) edits = edits.Where(e => e.EditDateTime < endDate);

            return Ok(new
            {
                edits = await edits.ToPaginatedListAsync(pageIndex, 30),
                editorUsername,
                editedType,
                editedId,
                editedField,
                oldData,
                newData,
                startDate,
                endDate
            });
        }

        [HttpGet]
        public async Task<IActionResult> DeleteHistory(
            string? deleterUsername,
            string? deletedType,
            int? deletedId,
            DateTime? startDate,
            DateTime? endDate,
            int pageIndex = 1)
        {
            var deletes = _context.DeletesHistory.Include(eh => eh.Deleter).AsNoTracking();

            if (!string.IsNullOrWhiteSpace(deleterUsername)) deletes = deletes.Where(d => d.Deleter.UserName!.Contains(deleterUsername));
            if (!string.IsNullOrWhiteSpace(deletedType)) deletes = deletes.Where(d => d.DeletedType.Contains(deletedType));
            if (deletedId.HasValue) deletes = deletes.Where(d => d.DeletedId == deletedId.Value);
            if (startDate is not null) deletes = deletes.Where(e => e.DeleteDateTime > startDate);
            if (endDate is not null) deletes = deletes.Where(e => e.DeleteDateTime < endDate);

            return Ok(new
            {
                deletes = await deletes.ToPaginatedListAsync(pageIndex, 30),
                deleterUsername,
                deletedType,
                deletedId,
                startDate,
                endDate
            });
        }

        [HttpGet]
        public async Task<IActionResult> ProductMetrics()
        {
            var searchedWords = _context.Searches.AsNoTracking()
                .Where(s => !string.IsNullOrWhiteSpace(s.KeyWord))
                .AsEnumerable()
                .SelectMany(s => s.KeyWord.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                .GroupBy(w => w)
                .Select(g => new KeyValuePair<string,int>(g.Key, g.Count()))
                .OrderByDescending(g => g.Value)
                .ToDictionary();

            var searchedCategoriesTask = _context.Searches
                .Include(s => s.Category)
                .AsNoTracking()
                .Where(s => s.Category != null)
                .GroupBy(s => s.Category!.Name)
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .ToDictionaryAsync(g => g.Category, g => g.Count);

            var productMetricsTask = _context.Products.AsNoTracking()
                .GroupJoin(_context.OrderProducts.AsNoTracking(), product => product.Id, op => op.Product.Id, (product, orderProducts) => new { product, orderProducts })
                .Select(pair => new
                {
                    pair.product.Id,
                    pair.product.Name,
                    TotalSales = pair.orderProducts.Sum(op => op.Quantity),
                    Revenue = pair.orderProducts.Sum(op => op.ProductPriceCents * (1 - op.SalePercent / 100.0) * op.Quantity),
                    StockLevel = pair.product.Quantity,
                    ProductViews = pair.product.Views,
                    CartCount = _context.CartProducts.AsNoTracking().Where(cp => cp.Product.Id == pair.product.Id).Count(),
                    WishlistCount = _context.Users.AsNoTracking().Count(u => u.WishList.Any(p => p.Id == pair.product.Id)),
                    ReturnedCount = _context.Returns
                        .Include(rpo => rpo.OrderProduct)
                            .ThenInclude(op => op.Product)
                        .AsNoTracking()
                        .Count(rpo => rpo.OrderProduct.Product.Id == pair.product.Id),
                    AverageRating = pair.product.Reviews.Any() ? pair.product.Reviews.Average(r => r.Rating) : 0,
                }).ToListAsync();

            await Task.WhenAll(searchedCategoriesTask, productMetricsTask);

            var searchedCategories = await searchedCategoriesTask;
            var productMetrics = await productMetricsTask;

            return Ok(new { productMetrics, searchedWords, searchedCategories });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> ProductMetrics(int id, DateTime? startingDate)
        {
            var product = await _context.Products
                .Include(p => p.Reviews)
                .Include(p => p.Category)
                .Include(p => p.EditsHistory)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product is null)
                return NotFound("Product not found!");

            var currentCartCountTask = _context.CartProducts
                .Include(cp => cp.Product)
                .AsNoTracking()
                .CountAsync(cp => cp.Product.Id == id);
            
            var currentWishlistCountTask = _context.Users
                .Include(u => u.WishList)
                .AsNoTracking()
                .CountAsync(u => u.WishList.Any(p => p.Id == id));

            var ordersQuery = _context.Orders
                .Include(o => o.OrderProducts)
                    .ThenInclude(op => op.Product)
                .AsNoTracking();

            var returnQuery = _context.Returns
                .Include(rpo => rpo.OrderProduct)
                    .ThenInclude(op => op.Product)
                .AsNoTracking()
                .Where(rpo => rpo.OrderProduct.Product.Id == id);

            var views = product.Views;
            var reviews = product.Reviews;
            if (startingDate is not null)
            {
                ordersQuery = ordersQuery.Where(o => o.CreatedDateTime > startingDate);
                returnQuery = returnQuery.Where(rpo => rpo.CreatedDateTime > startingDate);
                views = product.EditsHistory.Count(e => e.EditedField == nameof(product.Views) && e.EditDateTime > startingDate);
                reviews = reviews.Where(r => r.CreatedDateTime > startingDate).ToList();
            }

            var orderProductsQuery = ordersQuery
                .SelectMany(o => o.OrderProducts)
                .Where(op => op.Product.Id == id);

            var totalSalesTask = orderProductsQuery
                .SumAsync(op => op.Quantity);

            var revenueTask = orderProductsQuery
                .SumAsync(op => op.ProductPriceCents * (1 - op.SalePercent / 100.0) * op.Quantity);

            var returnCountTask = returnQuery.CountAsync();

            var ratings = new ulong[5];
            foreach (var review in reviews) ratings[review.Rating - 1]++;

            await Task.WhenAll(currentCartCountTask, currentWishlistCountTask, totalSalesTask, revenueTask, returnCountTask);

            var currentCartCount = await currentCartCountTask;
            var currentWishlistCount = await currentWishlistCountTask;
            var totalSales = await totalSalesTask;
            var revenue = await revenueTask;
            var returnCount = await returnCountTask;

            var productMetrics = new
            {
                product.Id,
                product.Name,
                currentCartCount,
                currentWishlistCount,
                views,
                product.Quantity,
                totalSales,
                revenue,
                returnCount,
                ratings
            };

            return Ok(new { productMetrics, startingDate });
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<IActionResult> Transporter()
        {
            var transporters = await _userManager.GetUsersInRoleAsync("Transporter");
            return Ok(transporters.Adapt<IList<UserDetailedResponse>>());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Transporter(string id)
        {
            var transporter = await _context.Users.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
            if (transporter is null)
                return NotFound("Transporter not found!");

            var ordersTask = _context.Orders
                .Include(o => o.Transporter)
                .Include(o => o.OrderProducts)
                    .ThenInclude(op => op.Product)
                .AsNoTracking()
                .Where(o => o.Transporter != null && o.Transporter.Id == id)
                .ToListAsync();

            var returnsTask = _context.Returns
                .Include(rpo => rpo.Transporter)
                .Include(rpo => rpo.Order)
                .Include(rpo => rpo.OrderProduct)
                    .ThenInclude(op => op.Product)
                .AsNoTracking()
                .Where(rpo => rpo.Transporter != null && rpo.Transporter.Id == id)
                .ToListAsync();

            await Task.WhenAll(ordersTask, returnsTask);

            var orders = await ordersTask;
            var returns = await returnsTask;

            return Ok(new { transporter = transporter.Adapt<UserPriefResponse>(), orders, returns });
        }
    }
}

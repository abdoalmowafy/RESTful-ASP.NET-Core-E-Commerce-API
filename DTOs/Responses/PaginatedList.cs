using Microsoft.EntityFrameworkCore;

namespace ECommerceAPI.DTOs.Responses
{
    public class PaginatedList<T>(IEnumerable<T> items, int count, int pageIndex, int pageSize) : List<T>(items)
    {
        public int PageIndex { get; private set; } = pageIndex;
        public int TotalPages { get; private set; } = (int)Math.Ceiling(count / (double)pageSize);

        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;
    }

    public static class PaginatedListExtensions
    {
        public async static Task<PaginatedList<T>> ToPaginatedListAsync<T>(this IQueryable<T> source, int pageIndex, int pageSize)
        {
            var count = await source.CountAsync();
            var items = source.Skip((pageIndex - 1) * pageSize).Take(pageSize);
            return new PaginatedList<T>(items, count, pageIndex, pageSize);
        }
        public static PaginatedList<T> ToPaginatedList<T>(this IEnumerable<T> source, int pageIndex, int pageSize)
        {
            var count = source.Count();
            var items = source.Skip((pageIndex - 1) * pageSize).Take(pageSize);
            return new PaginatedList<T>(items, count, pageIndex, pageSize);
        }
    }
}
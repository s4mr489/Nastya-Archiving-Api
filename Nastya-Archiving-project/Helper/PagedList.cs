using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Asn1.X509;

namespace Nastya_Archiving_project.Helper
{
    public class PagedList<T>
    {
        public PagedList(List<T> items, int pageNumber, int pageSize, int totalCount)
        {

        }

        public List<T> Items { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        //TODO-CONSIDER: Ignore deleted elements


        // Factory method to create a PagedList from an IQueryable
        public static async Task<PagedList<T>> Create(IQueryable<T> query , int pageNumber, int pageSize)
        {
            pageNumber = pageNumber < 1 ? 1 : pageNumber;
            pageSize = pageSize < 1 ? 10 : (pageSize > 15 ? pageSize : 15);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedList<T>(items, pageNumber, pageSize, totalCount);
        }
    }
}
using Nastya_Archiving_project.Helper;
using Nastya_Archiving_project.Models.DTOs;

namespace Nastya_Archiving_project.Extinstion
{
    public static class DbSetExtnsions
    {
        public static async Task<PagedList<T>> Paginate<T>(this IQueryable<T> query, BaseFilter filter)
        {
            if (query == null)
                return null;

            return await PagedList<T>.Create(query, filter.PageNumber, filter.PageSize);
        }
    }
}

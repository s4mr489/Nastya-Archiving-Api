using Nastya_Archiving_project.Models;
using Nastya_Archiving_project.Models.DTOs;
using Nastya_Archiving_project.Models.DTOs.Search;
using Nastya_Archiving_project.Models.DTOs.Search.DeletedDocsSearch;
using Nastya_Archiving_project.Models.DTOs.Search.QuikSearch;
using Nastya_Archiving_project.Models.DTOs.Search.UsersSearch;

namespace Nastya_Archiving_project.Services.search
{
    public interface ISearchServices
    {
        Task<(List<QuikSearchResponseDTOs>? docs, string? error)> QuikeSearch(QuikeSearchViewForm req);
        Task<(List<DetialisSearchResponseDTOs>? docs, string? error)> DetailsSearch(QuikeSearchViewForm req);
        Task<(List<QuikSearchResponseDTOs>? docs, string? error)> GetArcivingDocsAsync(
               string? docsNumber = null,
               string? subject = null,
               string? source = null,
               string? referenceTo = null,
               int? fileType = null,
               DateOnly? from = null,
               DateOnly? to = null,
               int pageNumber = 1,
               int pageSize = 20);
        Task<List<BaseResponseDTOs>> DeletedDocsSearch(SearchDeletedDocsViewForm search);

        Task<BaseResponseDTOs> PermissionSearch(UsersSearchViewForm search);
    }
}

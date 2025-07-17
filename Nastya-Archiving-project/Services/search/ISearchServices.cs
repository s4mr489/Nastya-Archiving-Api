using Nastya_Archiving_project.Models.DTOs;
using Nastya_Archiving_project.Models.DTOs.Search;
using Nastya_Archiving_project.Models.DTOs.Search.DeletedDocsSearch;
using Nastya_Archiving_project.Models.DTOs.Search.QuikSearch;

namespace Nastya_Archiving_project.Services.search
{
    public interface ISearchServices
    {
        Task<(List<QuikSearchResponseDTOs>? docs, string? error)> QuikeSearch(QuikeSearchViewForm req);
        Task<(List<DetialisSearchResponseDTOs>? docs, string? error)> DetailsSearch(QuikeSearchViewForm req);
        Task<List<BaseResponseDTOs>> DeletedDocsSearch(SearchDeletedDocsViewForm search);
    }
}

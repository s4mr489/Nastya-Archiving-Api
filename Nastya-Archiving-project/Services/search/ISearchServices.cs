using Nastya_Archiving_project.Models;
using Nastya_Archiving_project.Models.DTOs;
using Nastya_Archiving_project.Models.DTOs.ArchivingDocs.JoinedDocs;
using Nastya_Archiving_project.Models.DTOs.Search;
using Nastya_Archiving_project.Models.DTOs.Search.CasesSearch;
using Nastya_Archiving_project.Models.DTOs.Search.DeletedDocsSearch;
using Nastya_Archiving_project.Models.DTOs.Search.QuikSearch;
using Nastya_Archiving_project.Models.DTOs.Search.TreeSearch;
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
        //Note: this method is Nout used in the current codebase, but it is kept for case if we need it .
        Task<BaseResponseDTOs> PermissionSearch(UsersSearchViewForm search);
         
        /// <summary>This method is used to search for documents that the user has joined.</summary>
        Task<BaseResponseDTOs> SearchForJoinedDocs(string systemId);
        /// <summary>
        /// taht implmention for search 
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        //Task<BaseResponseDTOs> SearchForJoinedDocsFilter(QuikeSearchViewForm req);
        Task<BaseResponseDTOs> SearchForJoinedDocsFilter(QuikeSearchViewForm req);
        //that implmention used to return the search result like Tree 
        Task<BaseResponseDTOs> TreeSearch(TreeSearchViewForm req);
        //that implmention for cases Serach that make filter on the realated document 
        Task<BaseResponseDTOs> CasesSearch(CasesSearchViewForm req);
        //that implmention for azber search 
        Task<BaseResponseDTOs> AzberSearch(string azberNo);
        /// <summary>
        /// Returns all joined documents with their parent reference numbers and briefcase numbers
        /// </summary>
        /// <param name="req">Search parameters</param>
        /// <returns>BaseResponseDTOs containing all joined documents grouped by parent reference</returns>
        Task<BaseResponseDTOs> SearchForAllJoinedDocs(QuikeSearchViewForm req);
    }
}

using Nastya_Archiving_project.Models.DTOs;
using Nastya_Archiving_project.Models.DTOs.Statistically;

namespace Nastya_Archiving_project.Services.statistically
{
    public interface IStatisticallyServices
    {
        //this implmention for the statistical and its do
        Task<BaseResponseDTOs> GetCountByMonthAsync(StatisticallyViewForm req);
        Task<BaseResponseDTOs> GetFileSizeUplodedByMonthAsync(StatisticallyViewForm req);
        Task<BaseResponseDTOs> GetFileCountByEditorAsync(StatisticallyViewForm req);
        Task<BaseResponseDTOs> GetDocumentByDocType(StatisticallyViewForm req);
        Task<BaseResponseDTOs> GetDocumentBySupDocTpye(StatisticallyViewForm req);
        Task<BaseResponseDTOs> GetDocumentByOrgniztion(StatisticallyViewForm req);
        Task<BaseResponseDTOs> GetDocumentByDocTargetAsync(StatisticallyViewForm req);
        
        // New methods based on requirements
        Task<BaseResponseDTOs> GetFileSizeByEditorAsync(StatisticallyViewForm req);
        Task<BaseResponseDTOs> GetFileSizeByOrgnizationAsync(StatisticallyViewForm req);
        Task<BaseResponseDTOs> GetFileSizeByDocTargetAsync(StatisticallyViewForm req);
        Task<BaseResponseDTOs> GetFileSizeByDocTypeAsync(StatisticallyViewForm req);
        Task<BaseResponseDTOs> GetFileSizeBySupDocTypeAsync(StatisticallyViewForm req);
        Task<BaseResponseDTOs> CompareDocDateWithEditDateAsync(StatisticallyViewForm req);
    }
}

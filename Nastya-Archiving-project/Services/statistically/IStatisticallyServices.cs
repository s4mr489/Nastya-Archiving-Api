using Nastya_Archiving_project.Models.DTOs;
using Nastya_Archiving_project.Models.DTOs.Statistically;

namespace Nastya_Archiving_project.Services.statistically
{
    public interface IStatisticallyServices
    {

        Task<BaseResponseDTOs> GetCountByMonthAsync(StatisticallyViewForm req);
        Task<BaseResponseDTOs> GetFileSizeUplodedByMonthAsync(StatisticallyViewForm req);
        Task<BaseResponseDTOs> GeFileCountByEditorAsync(StatisticallyViewForm req);
        Task<BaseResponseDTOs> GetDocumentByDocType(StatisticallyViewForm req);
        Task<BaseResponseDTOs> GetDocumentBySupDocTpye(StatisticallyViewForm req);
        Task<BaseResponseDTOs> GetDocumentByOrgniztion(StatisticallyViewForm req);
    }
}

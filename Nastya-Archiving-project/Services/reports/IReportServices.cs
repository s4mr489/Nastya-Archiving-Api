using Microsoft.AspNetCore.Mvc;
using Nastya_Archiving_project.Models.DTOs;
using Nastya_Archiving_project.Models.DTOs.Reports;

namespace Nastya_Archiving_project.Services.reports
{
    public interface IReportServices
    {
       // Task<BaseResponseDTOs> GeneralResponse(ReportsViewForm req);
        Task<BaseResponseDTOs> GeneralReport(ReportsViewForm req);

        Task<BaseResponseDTOs> GetDepartmentDocumentCountsAsync(ReportsViewForm req);
        Task<BaseResponseDTOs> GetDepartmentDocumentsWithDetailsAsync(ReportsViewForm req);

        Task<BaseResponseDTOs> GetDepartmentEditorDocumentCountsPagedAsync(ReportsViewForm req);
        Task<BaseResponseDTOs> GetDepartmentEditorDocumentCountsPagedDetilesAsync(ReportsViewForm req);

        Task<BaseResponseDTOs> GetDepartmentMonthlyDocumentCountsPagedAsync(ReportsViewForm req);
        Task<BaseResponseDTOs> GetDepartmentMonthlyDocumentDetailsPagedAsync(ReportsViewForm req);

        Task<BaseResponseDTOs> GetSourceMonthlyDocumentCountsPagedAsync(ReportsViewForm req);
        Task<BaseResponseDTOs> GetSourceMonthlyDocumentDetailsPagedAsync(ReportsViewForm req);


        Task<BaseResponseDTOs> GetTargeteMonthlyDocumentCountsPagedAsync(ReportsViewForm req);
        Task<BaseResponseDTOs> GetTargetMonthlyDocumentDetailsPagedAsync(ReportsViewForm req);


        Task<BaseResponseDTOs> GetReferncesDocsDetailsPagedAsync(ReportsViewForm req);
        Task<BaseResponseDTOs> GetReferencedDocsCountsPagedAsync(ReportsViewForm req);

        Task<BaseResponseDTOs> CheckDocumentsFileIntegrityPagedAsync(int page, int pageSize);
        //Task<BaseResponseDTOs> CheckFilesAsync(ReportsViewForm req);
    }
}

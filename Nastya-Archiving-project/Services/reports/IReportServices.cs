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
    }
}

using Microsoft.AspNetCore.Mvc;
using Nastya_Archiving_project.Helper.Enums;
using Nastya_Archiving_project.Models.DTOs;
using Nastya_Archiving_project.Models.DTOs.Reports;
using Nastya_Archiving_project.Services.rdlcReport;
using Nastya_Archiving_project.Services.reports;
using static Nastya_Archiving_project.Services.reports.ResportServices;

namespace Nastya_Archiving_project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportController : ControllerBase
    {
        private readonly IReportServices _reportService;
        private readonly IRdlcReportServices _rdlcReportService;

        public ReportController(IReportServices reportService, IRdlcReportServices rdlcReportService)
        {
            _reportService = reportService;
            _rdlcReportService = rdlcReportService;
        }

        [HttpGet("General-Report")]
        public async Task<IActionResult> GeneralReport([FromQuery] ReportsViewForm req)
        {
            var result = await _reportService.GeneralReport(req);
            if (result.StatusCode == 200)
                return Ok(result);
            return BadRequest(result);
        }

        [HttpGet("department-Report")]
        public async Task<ActionResult<BaseResponseDTOs>> GetDepartmentDocumentCountsAsync([FromQuery] ReportsViewForm req)
        {
            BaseResponseDTOs result;
            if (req.resultType == EResultType.statistical)
            {
                result = await _reportService.GetDepartmentDocumentCountsAsync(req);
                return StatusCode(result.StatusCode, result);
            }
            result = await _reportService.GetDepartmentDocumentsWithDetailsAsync(req);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("department-by-users-Report")]
        public async Task<IActionResult> DepartmentBuUserReport([FromQuery] ReportsViewForm req)
        {
            BaseResponseDTOs result;
            if (req.resultType == EResultType.statistical)
            {
                result = await _reportService.GetDepartmentEditorDocumentCountsPagedAsync(req);
                return StatusCode(result.StatusCode, result);

            }

            result = await _reportService.GetDepartmentEditorDocumentCountsPagedDetilesAsync(req);
            return StatusCode(result.StatusCode, result);

        }

        [HttpGet("Deparment-by-monthly-Report")]
        public async Task<IActionResult> MonthlyDepartmenReport([FromQuery] ReportsViewForm req)
        {
            BaseResponseDTOs result;
            if (req.resultType == EResultType.statistical)
            {
                result = await _reportService.GetDepartmentMonthlyDocumentCountsPagedAsync(req);
                return StatusCode(result.StatusCode, result);
            }
            result = await _reportService.GetDepartmentMonthlyDocumentDetailsPagedAsync(req);
            return StatusCode(result.StatusCode, result);
        }


        [HttpGet("DocSource-Report")]
        public async Task<IActionResult> DepartmentByDocSourceReport([FromQuery] ReportsViewForm req)
        {
            BaseResponseDTOs result;
            if (req.resultType == EResultType.statistical)
            {
                result = await _reportService.GetSourceMonthlyDocumentCountsPagedAsync(req);
                return StatusCode(result.StatusCode, result);
            }
            result = await _reportService.GetSourceMonthlyDocumentDetailsPagedAsync(req);
            return StatusCode(result.StatusCode, result);
        }


        [HttpGet("DocTarget-Report")]
        public async Task<IActionResult> DepartmentByDocTargetReport([FromQuery] ReportsViewForm req)
        {
            BaseResponseDTOs result;
            if (req.resultType == EResultType.statistical)
            {
                result = await _reportService.GetTargeteMonthlyDocumentCountsPagedAsync(req);
                return StatusCode(result.StatusCode, result);
            }
            result = await _reportService.GetTargetMonthlyDocumentDetailsPagedAsync(req);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("Get-References-Docs")]
        public async Task<IActionResult> GetReferencesDocs([FromQuery] ReportsViewForm req)
        {
            BaseResponseDTOs result;
            if (req.resultType == EResultType.statistical)
            {
                result = await _reportService.GetReferencedDocsCountsPagedAsync(req);
                return StatusCode(result.StatusCode, result);
            }
            result = await _reportService.GetReferncesDocsDetailsPagedAsync(req);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("Get-MonthlyUsers-Report")]
        public async Task<IActionResult> GetMontlyUserReport([FromQuery] ReportsViewForm req)
        {
            BaseResponseDTOs result;
            if (req.resultType == EResultType.statistical)
            {
                result = await _reportService.GetMonthlyUsersDocumentCountPagedAsync(req);
                return StatusCode(result.StatusCode, result);
            }
            result = await _reportService.GetMontlyUsersDocumentDetailsPagedList(req);
            return StatusCode(result.StatusCode, result);
        }
        /// <summary>
        /// check documents file integrity with pagination
        /// </summary>
        /// <param name="pageSize"></param>
        /// <param name="pageNumber"></param>
        /// <param name="statusFilter">0: all , 1: not found , 2: damged , 3: non affected</param>
        /// <returns></returns>

        [HttpGet("Check-Documents-File-Integrity")]
        public async Task<IActionResult> CheckDocumentsFileIntegrity([FromQuery] int pageSize , [FromQuery] int pageNumber, FileIntegrityStatus statusFilter = FileIntegrityStatus.All)
        {
            BaseResponseDTOs result = await _reportService.CheckDocumentsFileIntegrityPagedAsync(pageNumber , pageSize, statusFilter);
            return StatusCode(result.StatusCode, result);
        }


        //[HttpGet("teste")]
        //public async Task<IActionResult> teste([FromQuery] ReportsViewForm req)
        //{
        //    BaseResponseDTOs result = await _reportServices.GetDocumentDetailsReportWithFastReport(req);
        //    return StatusCode(result.StatusCode, result);
        //}


        [HttpGet("archiving")]
        public async Task<IActionResult> GetArchiving([FromQuery] ReportFilter filter, [FromQuery] string format = "pdf", CancellationToken ct = default)
        {
            var bytes = await _rdlcReportService.GenerateReportAsync("GeneralReport", format, filter, ct);
            var contentType = format.ToLowerInvariant() switch
            {
                "pdf" => "application/pdf",
                "excel" => "application/vnd.ms-excel",
                "word" => "application/msword",
                "html" => "text/html",
                _ => "application/octet-stream"
            };
            var ext = format.Equals("html", StringComparison.OrdinalIgnoreCase) ? "html" :
                      format.Equals("excel", StringComparison.OrdinalIgnoreCase) ? "xls" :
                      format.Equals("word", StringComparison.OrdinalIgnoreCase) ? "doc" : "pdf";

            return File(bytes, contentType, $"Archiving_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{ext}");
        }

        //[HttpPost("rdlc-report")]
        //public async Task<IActionResult> GenerateRdlcReport([FromQuery] ReportsViewForm request, CancellationToken ct = default)
        //{
        //    var response = await _rdlcReportService.GenerateReportFromViewForm(request, ct);
            
        //    if (response.StatusCode != 200)
        //    {
        //        return StatusCode(response.StatusCode, response);
        //    }
            
        //    // The returned data should be byte[] representing the report
        //    var reportBytes = (byte[])response.Data;
            
        //    // Determine content type and extension based on requested format
        //    var format = request.outputFormat?.ToLower() ?? "pdf";
        //    var contentType = format switch
        //    {
        //        "pdf" => "application/pdf",
        //        "excel" => "application/vnd.ms-excel",
        //        "word" => "application/msword",
        //        "html" => "text/html",
        //        _ => "application/pdf"
        //    };
        //    var ext = format.Equals("html", StringComparison.OrdinalIgnoreCase) ? "html" :
        //              format.Equals("excel", StringComparison.OrdinalIgnoreCase) ? "xls" :
        //              format.Equals("word", StringComparison.OrdinalIgnoreCase) ? "doc" : "pdf";
            
        //    // Generate filename based on report type
        //    var reportTypeName = Enum.GetName(typeof(EReportType), request.reportType ?? EReportType.GeneralReport) ?? "Report";
        //    var filename = $"{reportTypeName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{ext}";
            
        //    return File(reportBytes, contentType, filename);
        //}
    }
}

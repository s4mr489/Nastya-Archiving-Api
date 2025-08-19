using DocumentFormat.OpenXml.Office2016.Drawing.Command;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nastya_Archiving_project.Helper;
using Nastya_Archiving_project.Helper.Enums;
using Nastya_Archiving_project.Models.DTOs;
using Nastya_Archiving_project.Models.DTOs.Reports;
using Nastya_Archiving_project.Services.infrastructure;
using Nastya_Archiving_project.Services.rdlcReport;
using Nastya_Archiving_project.Services.reports;

namespace Nastya_Archiving_project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportController : ControllerBase
    {
        private readonly IRdlcReportServices _svc;
        private readonly IReportServices _reportServices;
        private readonly IInfrastructureServices _infrastructureServices;
        private readonly ReportGenerator _reportGenerator;
        public ReportController(IReportServices reportServices, IRdlcReportServices svc)
        {
            _reportServices = reportServices;
            _svc = svc;
        }

        [HttpGet("General-Report")]
        public async Task<IActionResult> GeneralReport([FromQuery] ReportsViewForm req)
        {
            var result = await _reportServices.GeneralReport(req);
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
                result = await _reportServices.GetDepartmentDocumentCountsAsync(req);
                return StatusCode(result.StatusCode, result);
            }
            result = await _reportServices.GetDepartmentDocumentsWithDetailsAsync(req);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("department-by-users-Report")]
        public async Task<IActionResult> DepartmentBuUserReport([FromQuery] ReportsViewForm req)
        {
            BaseResponseDTOs result;
            if (req.resultType == EResultType.statistical)
            {
                result = await _reportServices.GetDepartmentEditorDocumentCountsPagedAsync(req);
                return StatusCode(result.StatusCode, result);

            }

            result = await _reportServices.GetDepartmentEditorDocumentCountsPagedDetilesAsync(req);
            return StatusCode(result.StatusCode, result);

        }

        [HttpGet("Deparment-by-monthly-Report")]
        public async Task<IActionResult> MonthlyDepartmenReport([FromQuery] ReportsViewForm req)
        {
            BaseResponseDTOs result;
            if (req.resultType == EResultType.statistical)
            {
                result = await _reportServices.GetDepartmentMonthlyDocumentCountsPagedAsync(req);
                return StatusCode(result.StatusCode, result);
            }
            result = await _reportServices.GetDepartmentMonthlyDocumentDetailsPagedAsync(req);
            return StatusCode(result.StatusCode, result);
        }


        [HttpGet("DocSource-Report")]
        public async Task<IActionResult> DepartmentByDocSourceReport([FromQuery] ReportsViewForm req)
        {
            BaseResponseDTOs result;
            if (req.resultType == EResultType.statistical)
            {
                result = await _reportServices.GetSourceMonthlyDocumentCountsPagedAsync(req);
                return StatusCode(result.StatusCode, result);
            }
            result = await _reportServices.GetSourceMonthlyDocumentDetailsPagedAsync(req);
            return StatusCode(result.StatusCode, result);
        }


        [HttpGet("DocTarget-Report")]
        public async Task<IActionResult> DepartmentByDocTargetReport([FromQuery] ReportsViewForm req)
        {
            BaseResponseDTOs result;
            if (req.resultType == EResultType.statistical)
            {
                result = await _reportServices.GetTargeteMonthlyDocumentCountsPagedAsync(req);
                return StatusCode(result.StatusCode, result);
            }
            result = await _reportServices.GetTargetMonthlyDocumentDetailsPagedAsync(req);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("Get-References-Docs")]
        public async Task<IActionResult> GetReferencesDocs([FromQuery] ReportsViewForm req)
        {
            BaseResponseDTOs result;
            if (req.resultType == EResultType.statistical)
            {
                result = await _reportServices.GetReferencedDocsCountsPagedAsync(req);
                return StatusCode(result.StatusCode, result);
            }
            result = await _reportServices.GetReferncesDocsDetailsPagedAsync(req);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("Get-MonthlyUsers-Report")]
        public async Task<IActionResult> GetMontlyUserReport([FromQuery] ReportsViewForm req)
        {
            BaseResponseDTOs result;
            if (req.resultType == EResultType.statistical)
            {
                result = await _reportServices.GetMonthlyUsersDocumentCountPagedAsync(req);
                return StatusCode(result.StatusCode, result);
            }
            result = await _reportServices.GetMontlyUsersDocumentDetailsPagedList(req);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("Check-Documents-File-Integrity")]
        public async Task<IActionResult> CheckDocumentsFileIntegrity([FromQuery] int pageSize , [FromQuery] int pageNumber)
        {
            BaseResponseDTOs result = await _reportServices.CheckDocumentsFileIntegrityPagedAsync(pageNumber , pageSize);
            return StatusCode(result.StatusCode, result);
        }


        [HttpGet("teste")]
        public async Task<IActionResult> teste([FromQuery] ReportsViewForm req)
        {
            BaseResponseDTOs result = await _reportServices.GetDocumentDetailsReportWithFastReport(req);
            return StatusCode(result.StatusCode, result);
        }


        [HttpGet("archiving")]
        public async Task<IActionResult> GetArchiving([FromQuery] ReportFilter filter, [FromQuery] string format = "pdf", CancellationToken ct = default)
        {
            var bytes = await _svc.GenerateReportAsync("Report1", format, filter, ct);
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
    }
}

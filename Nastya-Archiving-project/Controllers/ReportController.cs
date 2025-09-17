using Microsoft.AspNetCore.Mvc;
using Microsoft.Reporting.NETCore;
using Nastya_Archiving_project.Helper.Enums;
using Nastya_Archiving_project.Models.DTOs;
using Nastya_Archiving_project.Models.DTOs.Reports;
using Nastya_Archiving_project.Models.DTOs.Search.QuikSearch;
using Nastya_Archiving_project.Services.rdlcReport;
using Nastya_Archiving_project.Services.reports;
using Nastya_Archiving_project.Services.search;
using System.Text;
using static Nastya_Archiving_project.Services.reports.ResportServices;

namespace Nastya_Archiving_project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportController : ControllerBase
    {
        private readonly IReportServices _reportService;
        private readonly IRdlcReportServices _rdlcReportService;
        private readonly ISearchServices _searchServices;
        private readonly ILogger<ReportController> _logger;

        public ReportController(IReportServices reportService, IRdlcReportServices rdlcReportService, ISearchServices searchServices, ILogger<ReportController> logger)
        {
            _reportService = reportService;
            _rdlcReportService = rdlcReportService;
            _searchServices = searchServices;
            _logger = logger;
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


        [HttpGet("documents")]
        public async Task<IActionResult> GenerateDocumentsReport([FromQuery] QuikeSearchViewForm searchCriteria, string format = "PDF")
        {
            // Get data from search service
            var (docs, error) = await _searchServices.QuikeSearch(searchCriteria);

            if (error != null)
                return BadRequest(error);

            if (docs == null || docs.Count == 0)
                return NotFound("No documents found matching the search criteria.");

            try
            {
                // Set up the report - Use the correct folder name consistently
                string reportPath = Path.Combine(Directory.GetCurrentDirectory(), "Report", "DocumentReport.rdlc");

                // Check if file exists and log info
                if (!System.IO.File.Exists(reportPath))
                {
                    _logger.LogError($"Report file not found at: {reportPath}");
                    return NotFound($"Report template not found. Please check if '{reportPath}' exists.");
                }

                _logger.LogInformation($"Loading report from: {reportPath}");
                using var reportStream = new FileStream(reportPath, FileMode.Open);

                // Configure report parameters
                var parameters = new[] {
                    new ReportParameter("ReportTitle", "Documents Search Results"),
                    new ReportParameter("GeneratedDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
                    new ReportParameter("SearchCriteria", GetSearchCriteriaDescription(searchCriteria))
                };

                // Setup the report
                LocalReport report = new LocalReport();
                report.LoadReportDefinition(reportStream);
                report.DataSources.Add(new ReportDataSource("DocumentsDataSet", docs));
                report.SetParameters(parameters);

                // Render the report
                byte[] reportContent;
                string contentType;
                string fileExtension;

                switch (format.ToUpper())
                {
                    case "EXCEL":
                        reportContent = report.Render("EXCEL");
                        contentType = "application/vnd.ms-excel";
                        fileExtension = "xlsx";
                        break;
                    case "WORD":
                        reportContent = report.Render("WORD");
                        contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                        fileExtension = "docx";
                        break;
                    case "PDF":
                    default:
                        reportContent = report.Render("PDF");
                        contentType = "application/pdf";
                        fileExtension = "pdf";
                        break;
                }

                // Explicitly call the FileResult method from ControllerBase
                return base.File(reportContent, contentType, $"Documents_{DateTime.Now:yyyyMMddHHmmss}.{fileExtension}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating report");
                return StatusCode(500, $"Error generating report: {ex.Message}\nInner exception: {ex.InnerException?.Message}");
            }
        }

        [HttpGet("details-report")]
        public async Task<IActionResult> GenerateDetailsReport([FromQuery] QuikeSearchViewForm searchCriteria, string format = "PDF")
        {
            // Get detailed data from search service
            var (docs, error) = await _searchServices.DetailsSearch(searchCriteria);

            if (error != null)
                return BadRequest(error);

            if (docs == null || docs.Count == 0)
                return NotFound("No documents found matching the search criteria.");

            try
            {
                // Set up the report - Use the correct folder name consistently
                string reportPath = Path.Combine(Directory.GetCurrentDirectory(), "Report", "DocumentDetailsReport.rdlc");

                // Check if file exists and log info
                if (!System.IO.File.Exists(reportPath))
                {
                    _logger.LogError($"Report file not found at: {reportPath}");
                    return NotFound($"Report template not found. Please check if '{reportPath}' exists.");
                }

                _logger.LogInformation($"Loading report from: {reportPath}");
                using var reportStream = new FileStream(reportPath, FileMode.Open);

                // Configure report parameters
                var parameters = new[] {
                    new ReportParameter("ReportTitle", "Detailed Document Report"),
                    new ReportParameter("GeneratedDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
                    new ReportParameter("SearchCriteria", GetSearchCriteriaDescription(searchCriteria))
                };

                // Setup the report
                LocalReport report = new LocalReport();
                report.LoadReportDefinition(reportStream);
                report.DataSources.Add(new ReportDataSource("DocumentDetailsDataSet", docs));
                report.SetParameters(parameters);

                // Render the report
                byte[] reportContent;
                string contentType;
                string fileExtension;

                switch (format.ToUpper())
                {
                    case "EXCEL":
                        reportContent = report.Render("EXCELOPENXML"); // Use EXCELOPENXML instead of EXCEL
                        contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                        fileExtension = "xlsx";
                        break;
                    case "PDF":
                    default:
                        reportContent = report.Render("PDF");
                        contentType = "application/pdf";
                        fileExtension = "pdf";
                        break;
                }

                // Explicitly call the FileResult method from ControllerBase
                return base.File(reportContent, contentType, $"DocumentDetails_{DateTime.Now:yyyyMMddHHmmss}.{fileExtension}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating report");
                return StatusCode(500, $"Error generating report: {ex.Message}\nInner exception: {ex.InnerException?.Message}");
            }
        }

        private string GetSearchCriteriaDescription(QuikeSearchViewForm criteria)
        {
            StringBuilder sb = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(criteria.docsNumber))
                sb.Append($"Document #: {criteria.docsNumber}, ");

            if (!string.IsNullOrWhiteSpace(criteria.subject))
                sb.Append($"Subject: {criteria.subject}, ");

            if (criteria.from.HasValue)
                sb.Append($"From: {criteria.from.Value:yyyy-MM-dd}, ");

            if (criteria.to.HasValue)
                sb.Append($"To: {criteria.to.Value:yyyy-MM-dd}, ");

            if (sb.Length > 2)
                sb.Length -= 2; // Remove trailing comma and space

            return sb.Length > 0 ? sb.ToString() : "All documents";
        }
    }
}

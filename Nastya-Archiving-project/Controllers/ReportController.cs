using DocumentFormat.OpenXml.Office2016.Drawing.Command;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nastya_Archiving_project.Helper.Enums;
using Nastya_Archiving_project.Models.DTOs;
using Nastya_Archiving_project.Models.DTOs.Reports;
using Nastya_Archiving_project.Services.reports;

namespace Nastya_Archiving_project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportController : ControllerBase
    {
        private readonly IReportServices _reportServices;

        public ReportController(IReportServices reportServices)
        {
            _reportServices = reportServices;
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
    }
}

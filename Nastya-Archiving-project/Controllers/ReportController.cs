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
            if(req.resultType == EResultType.statistical)
            {
                result = await _reportServices.GetDepartmentEditorDocumentCountsPagedAsync(req);
                return StatusCode(result.StatusCode,result);

            }

            return BadRequest("Not finshed yet");
        }

    }
}

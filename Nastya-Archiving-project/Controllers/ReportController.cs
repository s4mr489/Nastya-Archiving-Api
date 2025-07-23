using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

        [HttpPost("search")]
        public async Task<IActionResult> SearchReport([FromBody] ReportsViewForm req)
        {
            var result = await _reportServices.GeneralReport(req);
            if (result.StatusCode == 200)
                return Ok(result);
            return BadRequest(result);
        }
    }
}

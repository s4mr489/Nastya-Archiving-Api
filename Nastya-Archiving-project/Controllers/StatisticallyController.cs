using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nastya_Archiving_project.Models;
using Nastya_Archiving_project.Models.DTOs;
using Nastya_Archiving_project.Models.DTOs.Statistically;
using Nastya_Archiving_project.Services.statistically;

namespace Nastya_Archiving_project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatisticallyController : ControllerBase
    {
        private readonly IStatisticallyServices _statisticallyServices;

        public StatisticallyController(IStatisticallyServices statisticallyServices)
        {
            _statisticallyServices = statisticallyServices;
        }

        [HttpGet("Get-Statistical-ByMonth")]
        public async Task<IActionResult> GetStatisticalByMonth([FromQuery]StatisticallyViewForm req)
        {
            BaseResponseDTOs result;
            result = await _statisticallyServices.GetCountByMonthAsync(req);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("Get-Statistical-ByFileSize")]
        public async Task<IActionResult> GetStatisticalByFileSize([FromQuery]StatisticallyViewForm req)
        {
            BaseResponseDTOs result;
            result = await _statisticallyServices.GetFileSizeUplodedByMonthAsync(req);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("Get-Statistical-BySupDocType")]
        public async Task<IActionResult> GetStatisticalBySupDocType([FromQuery]StatisticallyViewForm req)
        {
            BaseResponseDTOs result;
            result = await _statisticallyServices.GetDocumentBySupDocTpye(req);
            return StatusCode(result.StatusCode, result);
        }
    }
}

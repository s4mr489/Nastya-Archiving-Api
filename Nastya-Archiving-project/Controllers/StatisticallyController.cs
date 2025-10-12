using Microsoft.AspNetCore.Authorization;
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
    [Authorize]
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

        [HttpGet("Get-Statistical-ByDocType")]
        public async Task<IActionResult> GetStatisticalByDocType([FromQuery]StatisticallyViewForm req)
        {
            BaseResponseDTOs result;
            result = await _statisticallyServices.GetDocumentByDocType(req);
            return StatusCode(result.StatusCode, result);
        }


        [HttpGet("Get-Statistical-ByEditor")]
        public async Task<IActionResult> GetStatisticalByEditor([FromQuery]StatisticallyViewForm req)
        {
            BaseResponseDTOs result;
            result = await _statisticallyServices.GetFileCountByEditorAsync(req);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("Get-Statistical-ByOrgnization")]
        public async Task<IActionResult> GetStatisticalByOrgnization([FromQuery]StatisticallyViewForm req)
        {
            BaseResponseDTOs result;
            result = await _statisticallyServices.GetDocumentByOrgniztion(req);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("Get-Statistical-ByDocTarget")]
        public async Task<IActionResult> GetStatisticalByDocTarget([FromQuery]StatisticallyViewForm req)
        {
            BaseResponseDTOs result;
            result = await _statisticallyServices.GetDocumentByDocTargetAsync(req);
            return StatusCode(result.StatusCode, result);
        }

        // New endpoints for the additional statistical features

        [HttpGet("Get-Statistical-FileSizeByEditor")]
        public async Task<IActionResult> GetStatisticalFileSizeByEditor([FromQuery]StatisticallyViewForm req)
        {
            BaseResponseDTOs result;
            result = await _statisticallyServices.GetFileSizeByEditorAsync(req);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("Get-Statistical-FileSizeByOrganization")]
        public async Task<IActionResult> GetStatisticalFileSizeByOrganization([FromQuery]StatisticallyViewForm req)
        {
            BaseResponseDTOs result;
            result = await _statisticallyServices.GetFileSizeByOrgnizationAsync(req);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("Get-Statistical-FileSizeByDocTarget")]
        public async Task<IActionResult> GetStatisticalFileSizeByDocTarget([FromQuery]StatisticallyViewForm req)
        {
            BaseResponseDTOs result;
            result = await _statisticallyServices.GetFileSizeByDocTargetAsync(req);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("Get-Statistical-FileSizeByDocType")]
        public async Task<IActionResult> GetStatisticalFileSizeByDocType([FromQuery]StatisticallyViewForm req)
        {
            BaseResponseDTOs result;
            result = await _statisticallyServices.GetFileSizeByDocTypeAsync(req);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("Get-Statistical-FileSizeBySupDocType")]
        public async Task<IActionResult> GetStatisticalFileSizeBySupDocType([FromQuery]StatisticallyViewForm req)
        {
            BaseResponseDTOs result;
            result = await _statisticallyServices.GetFileSizeBySupDocTypeAsync(req);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("Get-Statistical-CompareDocDateWithEditDate")]
        public async Task<IActionResult> GetStatisticalCompareDocDateWithEditDate([FromQuery]StatisticallyViewForm req)
        {
            BaseResponseDTOs result;
            result = await _statisticallyServices.CompareDocDateWithEditDateAsync(req);
            return StatusCode(result.StatusCode, result);
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Nastya_Archiving_project.Models;
using Nastya_Archiving_project.Models.DTOs;
using Nastya_Archiving_project.Models.DTOs.Search.DeletedDocsSearch;
using Nastya_Archiving_project.Models.DTOs.Search.QuikSearch;
using Nastya_Archiving_project.Models.DTOs.Search.UsersSearch;
using Nastya_Archiving_project.Services.search;

namespace Nastya_Archiving_project.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SearchController : ControllerBase
    {
        private readonly ISearchServices _searchServices;

        public SearchController(ISearchServices searchServices)
        {
            _searchServices = searchServices;
        }

        [HttpGet("quick-search")]
        public async Task<IActionResult> QuickSearch([FromQuery] QuikeSearchViewForm request)
        {
            var (docs, error) = await _searchServices.QuikeSearch(request);
            if (!string.IsNullOrEmpty(error))
                return NotFound(new { error });

            return Ok(docs);
        }

        [HttpGet("Details-search")]
        public async Task<IActionResult> DetialsSearch([FromQuery] QuikeSearchViewForm request)
        {
            var (docs, error) = await _searchServices.DetailsSearch(request);
            if (!string.IsNullOrEmpty(error))
                return NotFound(new { error });

            return Ok(docs);
        }

        [HttpGet("deleted-docs-search")]
        public async Task<IActionResult> DeletedDocsSearch([FromQuery]SearchDeletedDocsViewForm search)
        {
            var result = await _searchServices.DeletedDocsSearch(search);
            return Ok(result);
        }

        [HttpGet("arciving-docs")]
        public async Task<IActionResult> GetArcivingDocs(
        string? docsNumber,
        string? subject,
        string? source,
        string? referenceTo,
        int? fileType,
        DateOnly? from,
        DateOnly? to,
        int pageNumber = 1,
        int pageSize = 20)
        {
            var (docs, error) = await _searchServices.GetArcivingDocsAsync(
                docsNumber, subject, source, referenceTo, fileType, from, to, pageNumber, pageSize);

            if (error != null)
                return BadRequest(new { error });

            return Ok(docs);
        }


        //Not Used anyMore 
        //[HttpGet("permission-search")]
        //public async Task<IActionResult> PermissionSearch([FromQuery] UsersSearchViewForm search)
        //{
        //    var result = await _searchServices.PermissionSearch(search);
        //    return Ok(result);
        //}
    }
}

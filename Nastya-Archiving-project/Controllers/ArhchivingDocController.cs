using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nastya_Archiving_project.Models.DTOs.ArchivingDocs;
using Nastya_Archiving_project.Models.DTOs.ArchivingDocs.JoinedDocs;
using Nastya_Archiving_project.Models.DTOs.file;
using Nastya_Archiving_project.Services.archivingDocs;
using Nastya_Archiving_project.Services.SystemInfo;

namespace Nastya_Archiving_project.Controllers
{
    /// <summary>
    /// Controller for managing archiving document operations.
    /// </summary>
    /// <remarks>
    /// This controller handles all document archiving operations including:
    /// - Adding documents to the archive
    /// - Retrieving archived documents
    /// - Updating archived documents
    /// - Removing documents from the archive
    /// - Managing document relationships (joining, unbinding)
    /// - Handling deleted document restoration
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class ArhchivingDocController : ControllerBase
    {
        private readonly IArchivingDocsSercvices _archivingDocsSercvices;
        private readonly ISystemInfoServices _systemInfoServcies;
        public ArhchivingDocController(IArchivingDocsSercvices archivingDocsSercvices, ISystemInfoServices systemInfoServcies)
        {
            _archivingDocsSercvices = archivingDocsSercvices;
            _systemInfoServcies = systemInfoServcies;
        }

        [HttpPost("Add-Docs-To-Archive")]
        public async Task<IActionResult> PostArchivingDocs([FromForm] FileViewForm file, [FromForm] ArchivingDocsViewForm req)
        {
            var (docs, error) = await _archivingDocsSercvices.PostArchivingDocs(req, file);
            if (error != null)
                return BadRequest(error);
            return Ok(docs);
        }

        /// <summary>
        /// Restore a deleted document by its Id.
        /// </summary>
        [HttpPost("Restore-Deleted-Docs")]
        public async Task<IActionResult> RestoreDeletedDocuments(int Id)
        {
            var result = await _archivingDocsSercvices.RestoreDeletedDocuments(Id);
            if (result == "404")
                return NotFound("Document not found.");
            return Ok("Document restored successfully.");
        }

        [HttpDelete("Remove-Docs")]
        public async Task<IActionResult> DeleteArchivingDoc(int id)
        {
            var result = await _archivingDocsSercvices.DeleteArchivingDocs(id);
            if (result == "404")
                return NotFound("Document not found.");
            if (result == "403")
                // Use status code with an object containing the message instead
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not have permission to delete this document." });
            if (result == "401")
                return Unauthorized();
            return Ok("Document deleted successfully.");
        }

        [HttpGet("Get-Last-RefNo")]
        public async Task<IActionResult> GetLastRefNo()
        {
            var lastRefNo = await _systemInfoServcies.GetLastRefNo();
            return Ok(lastRefNo);
        }


        // <summary> This to edit the docs <summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> EditArchivingDoc(
        int id,
        [FromForm] ArchivingDocsViewForm req)
        {
            var (docs, error) = await _archivingDocsSercvices.EditArchivingDocs(req, id);
            if (error != null)
                return BadRequest(new { error });

            return Ok(docs);
        }
        /// <summary>
        /// Unbind and remove a document from the archive by its system reference number.
        /// </summary>
        [HttpDelete("Unbind-Doc-From-Archive/{systemId}")]
        public async Task<IActionResult> UnbindDocFromArchive(string systemId)
        {
            var (docs, error) = await _archivingDocsSercvices.UnbindDoucFromTheArchive(systemId);
            if (error != null)
                return NotFound(new { error });
            return Ok(docs);
        }

        //<summary> This Implmention for joined the docs</summary>
        [HttpPost("join-docs-from-archive")]
        public async Task<IActionResult> JoinDocsFromArchive([FromBody] JoinedDocsViewForm req)
        {
            var result = await _archivingDocsSercvices.JoinDocsFromArchive(req);
            if (result.StatusCode == 404)
                return NotFound(result);

            return Ok(result);
        }


        /// <summary>
        /// Gets image URLs from archived documents with pagination support
        /// </summary>
        /// <param name="page">Page number (1-based)</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <param name="getLastImage">If true, returns only the last image</param>
        /// <param name="getFirstImage">If true, returns only the first image</param>
        /// <returns>Paginated list of image URLs or specific image based on parameters</returns>
        [HttpGet("images")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetArchivingDocImages(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] bool getLastImage = false,
            [FromQuery] bool getFirstImage = false)
        {
            try
            {
                var (imageUrls, error, totalCount) = await _archivingDocsSercvices.GetArchivingDocImages(
                    page, pageSize, getLastImage, getFirstImage);

                if (imageUrls == null)
                {
                    return StatusCode(500, new { error });
                }

                if (imageUrls.Count == 0)
                {
                    return NotFound(new { message = error ?? "No images found" });
                }

                // Calculate pagination metadata
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                var hasNext = page < totalPages;
                var hasPrevious = page > 1;

                return Ok(new
                {
                    data = imageUrls,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize = pageSize,
                        totalCount,
                        totalPages,
                        hasNext,
                        hasPrevious
                    },
                    message = getLastImage ? "Last image retrieved successfully" :
                             getFirstImage ? "First image retrieved successfully" :
                             "Images retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"An error occurred: {ex.Message}" });
            }
        }

        [HttpPatch("Unbind-All-Docs-From-Parent/{parentSystemId}")]
        public async Task<IActionResult> UnbindAllDocsFromTheParent(string parentSystemId)
        {
            var result = await _archivingDocsSercvices.UnbindDoucAllDocsFromTheParent(parentSystemId);
            return StatusCode(result.StatusCode, result);
        }
        
        /// <summary>
        /// Gets a document by its reference number with all related details
        /// </summary>
        /// <param name="referenceNo">The reference number of the document to retrieve</param>
        /// <returns>Detailed document information including related data</returns>
        [HttpGet("by-reference/{referenceNo}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDocumentByReferenceNo(string referenceNo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(referenceNo))
                {
                    return BadRequest(new { error = "Reference number cannot be empty." });
                }

                var result = await _archivingDocsSercvices.GetDocumentByReferenceNo(referenceNo);
                
                // Return appropriate status code based on the response
                return StatusCode(result.StatusCode, result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"An unexpected error occurred: {ex.Message}" });
            }
        }
    }
}

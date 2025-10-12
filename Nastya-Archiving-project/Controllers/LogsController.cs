using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nastya_Archiving_project.Models.DTOs.Logs;
using Nastya_Archiving_project.Services.logs;

namespace Nastya_Archiving_project.Controllers
{

    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class LogsController : ControllerBase
    {
        private readonly ILogsServices _logsServices;

        public LogsController(ILogsServices logsServices)
        {
            _logsServices = logsServices;
        }

        /// <summary>
        /// Gets paginated log entries with optional filtering
        /// </summary>
        /// <param name="filter">Filter parameters</param>
        /// <returns>Filtered log entries</returns>
        [HttpGet]
        public async Task<IActionResult> GetLogs([FromQuery] LogsFilterDTO filter)
        {
            var result = await _logsServices.GetLogs(filter);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Gets detailed information about a specific log entry
        /// </summary>
        /// <param name="id">ID of the log entry</param>
        /// <returns>Log entry details</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetLogById(int id)
        {
            var result = await _logsServices.GetLogById(id);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Gets logs related to a specific document
        /// </summary>
        /// <param name="documentId">ID of the document</param>
        /// <param name="pageNumber">Page number (1-based)</param>
        /// <param name="pageSize">Items per page</param>
        /// <returns>Document logs</returns>
        [HttpGet("document/{documentId}")]
        public async Task<IActionResult> GetDocumentLogs(int documentId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _logsServices.GetDocumentLogs(documentId, pageNumber, pageSize);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Gets logs related to a document by its reference number
        /// </summary>
        /// <param name="referenceNo">Reference number of the document</param>
        /// <param name="pageNumber">Page number (1-based)</param>
        /// <param name="pageSize">Items per page</param>
        /// <returns>Document logs</returns>
        [HttpGet("document/reference/{referenceNo}")]
        public async Task<IActionResult> GetDocumentLogsByReference(string referenceNo, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _logsServices.GetDocumentLogsByReference(referenceNo, pageNumber, pageSize);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Gets update history of a document by reference number showing versions before updates
        /// </summary>
        /// <param name="referenceNo">Reference number of the document</param>
        /// <param name="pageNumber">Page number (1-based)</param>
        /// <param name="pageSize">Items per page</param>
        /// <param name="includeOriginal">Whether to include the original document state</param>
        /// <returns>Document update history</returns>
        [HttpGet("document/history/{referenceNo}")]
        public async Task<IActionResult> GetDocumentUpdateHistory(
            string referenceNo, 
            [FromQuery] int pageNumber = 1, 
            [FromQuery] int pageSize = 20,
            [FromQuery] bool includeOriginal = true)
        {
            var result = await _logsServices.GetDocumentUpdateHistory(referenceNo, pageNumber, pageSize, includeOriginal);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Gets user activity logs
        /// </summary>
        /// <param name="editor">Username</param>
        /// <param name="pageNumber">Page number (1-based)</param>
        /// <param name="pageSize">Items per page</param>
        /// <returns>User activity logs</returns>
        [HttpGet("user/{editor}")]
        public async Task<IActionResult> GetUserActivityLogs(string editor, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _logsServices.GetUserActivityLogs(editor, pageNumber, pageSize);
            return StatusCode(result.StatusCode, result);
        }
    }
}
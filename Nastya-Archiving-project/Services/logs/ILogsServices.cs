using Nastya_Archiving_project.Models.DTOs;
using Nastya_Archiving_project.Models.DTOs.Logs;

namespace Nastya_Archiving_project.Services.logs
{
    /// <summary>
    /// Interface for service that provides access to system logs
    /// </summary>
    public interface ILogsServices
    {
        /// <summary>
        /// Gets paginated log entries with optional filtering
        /// </summary>
        /// <param name="filter">Filter parameters including pagination options</param>
        /// <returns>BaseResponseDTOs containing the filtered logs</returns>
        Task<BaseResponseDTOs> GetLogs(LogsFilterDTO filter);

        /// <summary>
        /// Gets detailed information about a specific log entry by ID
        /// </summary>
        /// <param name="id">ID of the log entry</param>
        /// <returns>BaseResponseDTOs containing the log entry details</returns>
        Task<BaseResponseDTOs> GetLogById(int id);

        /// <summary>
        /// Gets logs related to a specific document
        /// </summary>
        /// <param name="documentId">ID of the document</param>
        /// <param name="pageNumber">Page number for pagination (1-based)</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <returns>BaseResponseDTOs containing logs related to the document</returns>
        Task<BaseResponseDTOs> GetDocumentLogs(int documentId, int pageNumber = 1, int pageSize = 20);

        /// <summary>
        /// Gets logs related to a specific document by reference number
        /// </summary>
        /// <param name="referenceNo">Reference number of the document</param>
        /// <param name="pageNumber">Page number for pagination (1-based)</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <returns>BaseResponseDTOs containing logs related to the document</returns>
        Task<BaseResponseDTOs> GetDocumentLogsByReference(string referenceNo, int pageNumber = 1, int pageSize = 20);

        /// <summary>
        /// Gets update history of a document by reference number with versions before updates, including original state
        /// </summary>
        /// <param name="referenceNo">Reference number of the document</param>
        /// <param name="pageNumber">Page number for pagination (1-based)</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <param name="includeOriginal">Whether to include the original document state (from Add operation)</param>
        /// <returns>BaseResponseDTOs containing document versions before updates</returns>
        Task<BaseResponseDTOs> GetDocumentUpdateHistory(string referenceNo, int pageNumber = 1, int pageSize = 20, bool includeOriginal = true);

        /// <summary>
        /// Gets user activity logs for a specific user
        /// </summary>
        /// <param name="editor">Username of the user</param>
        /// <param name="pageNumber">Page number for pagination (1-based)</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <returns>BaseResponseDTOs containing logs of the user's activity</returns>
        Task<BaseResponseDTOs> GetUserActivityLogs(string editor, int pageNumber = 1, int pageSize = 20);
    }
}
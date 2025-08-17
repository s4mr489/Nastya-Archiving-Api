using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Nastya_Archiving_project.Data;
using Nastya_Archiving_project.Models;
using Nastya_Archiving_project.Models.DTOs;
using Nastya_Archiving_project.Models.DTOs.Logs;
using System.Text;

namespace Nastya_Archiving_project.Services.logs
{
    /// <summary>
    /// Implementation of the ILogsServices interface for accessing system logs
    /// </summary>
    public class LogsServices : BaseServices, ILogsServices
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;

        /// <summary>
        /// Constructor for LogsServices
        /// </summary>
        /// <param name="context">Database context</param>
        /// <param name="mapper">AutoMapper instance</param>
        public LogsServices(AppDbContext context, IMapper mapper) : base(mapper, context)
        {
            _context = context;
            _mapper = mapper;
        }

        /// <summary>
        /// Gets paginated log entries with optional filtering
        /// </summary>
        /// <param name="filter">Filter parameters including pagination options</param>
        /// <returns>BaseResponseDTOs containing the filtered logs</returns>
        public async Task<BaseResponseDTOs> GetLogs(LogsFilterDTO filter)
        {
            try
            {
                // Start with a base query
                var query = _context.UsersEditings.AsQueryable();

                // Apply filters if provided
                if (!string.IsNullOrWhiteSpace(filter.OperationType))
                {
                    query = query.Where(log => log.OperationType == filter.OperationType);
                }

                if (!string.IsNullOrWhiteSpace(filter.Model))
                {
                    query = query.Where(log => log.Model == filter.Model);
                }

                if (!string.IsNullOrWhiteSpace(filter.TableName))
                {
                    query = query.Where(log => log.TblName == filter.TableName);
                }

                if (!string.IsNullOrWhiteSpace(filter.RecordId))
                {
                    query = query.Where(log => log.RecordId == filter.RecordId);
                }

                if (!string.IsNullOrWhiteSpace(filter.Editor))
                {
                    query = query.Where(log => log.Editor == filter.Editor);
                }

                if (filter.StartDate.HasValue)
                {
                    query = query.Where(log => log.EditDate >= filter.StartDate.Value);
                }

                if (filter.EndDate.HasValue)
                {
                    // Add one day to include the end date fully
                    var endDatePlusOneDay = filter.EndDate.Value.AddDays(1);
                    query = query.Where(log => log.EditDate < endDatePlusOneDay);
                }

                // Order by most recent first
                query = query.OrderByDescending(log => log.EditDate);

                // Get total count for pagination
                int totalCount = await query.CountAsync();
                int totalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize);

                // Apply pagination
                var pagedLogs = await query
                    .Skip((filter.PageNumber - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .ToListAsync();

                // Map to DTOs
                var logEntries = pagedLogs.Select(log => new LogEntryDTO
                {
                    Id = log.Id,
                    Model = log.Model,
                    TableName = log.TblName,
                    TableNameArabic = log.TblNameA,
                    RecordId = log.RecordId,
                    OperationType = log.OperationType,
                    Editor = log.Editor,
                    EditDate = log.EditDate,
                    IpAddress = log.Ipadress,
                    AccountUnitId = log.AccountUnitId
                }).ToList();

                // Return response with pagination info
                var response = new
                {
                    Logs = logEntries,
                    TotalCount = totalCount,
                    TotalPages = totalPages,
                    PageNumber = filter.PageNumber,
                    PageSize = filter.PageSize
                };

                return new BaseResponseDTOs(response, 200, null);
            }
            catch (Exception ex)
            {
                return new BaseResponseDTOs(null, 500, $"Error retrieving logs: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets detailed information about a specific log entry by ID
        /// </summary>
        /// <param name="id">ID of the log entry</param>
        /// <returns>BaseResponseDTOs containing the log entry details</returns>
        public async Task<BaseResponseDTOs> GetLogById(int id)
        {
            try
            {
                var log = await _context.UsersEditings.FirstOrDefaultAsync(l => l.Id == id);
                if (log == null)
                {
                    return new BaseResponseDTOs(null, 404, "Log entry not found");
                }

                // Create detailed DTO with parsed record data
                var detailedLog = new LogEntryDetailedDTO
                {
                    Id = log.Id,
                    Model = log.Model,
                    TableName = log.TblName,
                    TableNameArabic = log.TblNameA,
                    RecordId = log.RecordId,
                    OperationType = log.OperationType,
                    Editor = log.Editor,
                    EditDate = log.EditDate,
                    IpAddress = log.Ipadress,
                    AccountUnitId = log.AccountUnitId,
                    RawRecordData = log.RecordData,
                    ParsedRecordData = ParseRecordData(log.RecordData)
                };

                return new BaseResponseDTOs(detailedLog, 200, null);
            }
            catch (Exception ex)
            {
                return new BaseResponseDTOs(null, 500, $"Error retrieving log entry: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets logs related to a specific document
        /// </summary>
        /// <param name="documentId">ID of the document</param>
        /// <param name="pageNumber">Page number for pagination (1-based)</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <returns>BaseResponseDTOs containing logs related to the document</returns>
        public async Task<BaseResponseDTOs> GetDocumentLogs(int documentId, int pageNumber = 1, int pageSize = 20)
        {
            try
            {
                // Query logs related to this document ID
                var query = _context.UsersEditings
                    .Where(log => log.TblName == "Arciving_Docs" && log.RecordId == documentId.ToString())
                    .OrderByDescending(log => log.EditDate);

                // Get total count
                int totalCount = await query.CountAsync();
                int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                // Apply pagination
                var pagedLogs = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Map and enrich logs
                var logEntries = new List<LogEntryDetailedDTO>();
                foreach (var log in pagedLogs)
                {
                    logEntries.Add(new LogEntryDetailedDTO
                    {
                        Id = log.Id,
                        Model = log.Model,
                        TableName = log.TblName,
                        TableNameArabic = log.TblNameA,
                        RecordId = log.RecordId,
                        OperationType = log.OperationType,
                        Editor = log.Editor,
                        EditDate = log.EditDate,
                        IpAddress = log.Ipadress,
                        AccountUnitId = log.AccountUnitId,
                        RawRecordData = log.RecordData,
                        ParsedRecordData = ParseRecordData(log.RecordData)
                    });
                }

                var response = new
                {
                    DocumentId = documentId,
                    Logs = logEntries,
                    TotalCount = totalCount,
                    TotalPages = totalPages,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                return new BaseResponseDTOs(response, 200, null);
            }
            catch (Exception ex)
            {
                return new BaseResponseDTOs(null, 500, $"Error retrieving document logs: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets logs related to a specific document by reference number
        /// </summary>
        /// <param name="referenceNo">Reference number of the document</param>
        /// <param name="pageNumber">Page number for pagination (1-based)</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <returns>BaseResponseDTOs containing logs related to the document</returns>
        public async Task<BaseResponseDTOs> GetDocumentLogsByReference(string referenceNo, int pageNumber = 1, int pageSize = 20)
        {
            try
            {
                // Find the document ID from the reference number
                var document = await _context.ArcivingDocs
                    .FirstOrDefaultAsync(d => d.RefrenceNo == referenceNo);

                if (document == null)
                {
                    // Try finding in deleted documents
                    var deletedDocument = await _context.ArcivingDocsDeleteds
                        .FirstOrDefaultAsync(d => d.RefrenceNo == referenceNo);

                    if (deletedDocument == null)
                    {
                        return new BaseResponseDTOs(null, 404, $"Document with reference number {referenceNo} not found");
                    }

                    // Use the ID from the deleted document
                    return await GetDocumentLogs(deletedDocument.Id, pageNumber, pageSize);
                }

                // Use the ID from the active document
                return await GetDocumentLogs(document.Id, pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                return new BaseResponseDTOs(null, 500, $"Error retrieving document logs: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets update history of a document by reference number with only the changes between versions
        /// </summary>
        /// <param name="referenceNo">Reference number of the document</param>
        /// <param name="pageNumber">Page number for pagination (1-based)</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <param name="includeOriginal">Whether to include the original document state (from Add operation)</param>
        /// <returns>BaseResponseDTOs containing document changes between versions</returns>
        public async Task<BaseResponseDTOs> GetDocumentUpdateHistory(string referenceNo, int pageNumber = 1, int pageSize = 20, bool includeOriginal = true)
        {
            try
            {
                // First, find the document to get its reference number or ID
                var document = await _context.ArcivingDocs
                    .FirstOrDefaultAsync(d => d.RefrenceNo == referenceNo);

                if (document == null)
                {
                    // Check if it's in deleted documents
                    var deletedDocument = await _context.ArcivingDocsDeleteds
                        .FirstOrDefaultAsync(d => d.RefrenceNo == referenceNo);

                    if (deletedDocument == null)
                    {
                        return new BaseResponseDTOs(null, 404, $"Document with reference number {referenceNo} not found");
                    }

                    // Use deleted document ID
                    document = new ArcivingDoc { Id = deletedDocument.Id, RefrenceNo = deletedDocument.RefrenceNo };
                }

                // Get all logs for this document, including both Add and Update operations
                var allLogs = await _context.UsersEditings
                    .Where(log => 
                        log.TblName == "Arciving_Docs" && 
                        log.RecordId == document.RefrenceNo &&
                        (log.OperationType == "Update" || (includeOriginal && log.OperationType == "Add")))
                    .OrderByDescending(log => log.EditDate) // Most recent first
                    .ToListAsync();

                // Check if there are any logs
                if (allLogs.Count == 0)
                {
                    // If we couldn't find any logs with Add or Update, try to find other operations
                    var anyLogs = await _context.UsersEditings
                        .Where(log => 
                            log.TblName == "Arciving_Docs" && 
                            log.RecordId == document.RefrenceNo)
                        .OrderByDescending(log => log.EditDate)
                        .FirstOrDefaultAsync();

                    if (anyLogs != null)
                    {
                        return new BaseResponseDTOs(new
                        {
                            ReferenceNo = referenceNo,
                            DocumentId = document.Id,
                            Message = $"No update or creation history found, but found operations of type: {anyLogs.OperationType}",
                            UpdateCount = 0,
                            Updates = new List<object>()
                        }, 200, null);
                    }

                    return new BaseResponseDTOs(new
                    {
                        ReferenceNo = referenceNo,
                        DocumentId = document.Id,
                        Message = "No history found for this document",
                        UpdateCount = 0,
                        Updates = new List<object>()
                    }, 200, null);
                }

                // Separate logs by operation type
                var addLogs = allLogs.Where(log => log.OperationType == "Add").ToList();
                var updateLogs = allLogs.Where(log => log.OperationType == "Update").ToList();

                // Get total count and calculate pages
                int totalCount = allLogs.Count;
                int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                // Apply pagination
                var pagedLogs = allLogs
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Process each log to extract changes
                var documentHistory = new List<object>();
                
                // Sort logs chronologically (oldest first) to calculate changes between consecutive versions
                var chronologicalLogs = new List<UsersEditing>(pagedLogs);
                chronologicalLogs.Reverse();
                
                Dictionary<string, string>? previousState = null;
                
                foreach (var log in chronologicalLogs)
                {
                    // Parse the current record data
                    var currentState = ParseRecordData(log.RecordData);
            
                    // For the first log (Add operation or oldest), show the full state
                    if (previousState == null)
                    {
                        documentHistory.Add(new
                        {
                            LogId = log.Id,
                            OperationType = log.OperationType,
                            Editor = log.Editor,
                            OperationDate = log.EditDate,
                            IpAddress = log.Ipadress,
                            Changes = log.OperationType == "Add" ? 
                                new { FullState = currentState } : 
                                (object)CalculateChanges(new Dictionary<string, string>(), currentState)
                        });
                    }
                    else
                    {
                        // For subsequent logs, calculate and show only the changes
                        var changes = CalculateChanges(previousState, currentState);
                        
                        // Create a document version entry with changes only
                        documentHistory.Add(new
                        {
                            LogId = log.Id,
                            OperationType = log.OperationType,
                            Editor = log.Editor,
                            OperationDate = log.EditDate,
                            IpAddress = log.Ipadress,
                            Changes = changes
                        });
                    }
            
                    // Update previous state for next iteration
                    previousState = currentState;
                }
            
                // Reverse back to most recent first for the response
                documentHistory.Reverse();

                // Determine if we have the original state (Add operation)
                bool hasOriginalState = addLogs.Count > 0;

                // Create the response
                var response = new
                {
                    ReferenceNo = referenceNo,
                    DocumentId = document.Id,
                    HasOriginalState = hasOriginalState,
                    TotalHistoryCount = totalCount,
                    UpdateCount = updateLogs.Count,
                    History = documentHistory,
                    TotalPages = totalPages,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                return new BaseResponseDTOs(response, 200, null);
            }
            catch (Exception ex)
            {
                return new BaseResponseDTOs(null, 500, $"Error retrieving document history: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets user activity logs for a specific user
        /// </summary>
        /// <param name="editor">Username of the user</param>
        /// <param name="pageNumber">Page number for pagination (1-based)</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <returns>BaseResponseDTOs containing logs of the user's activity</returns>
        public async Task<BaseResponseDTOs> GetUserActivityLogs(string editor, int pageNumber = 1, int pageSize = 20)
        {
            try
            {
                // Query logs for this user
                var query = _context.UsersEditings
                    .Where(log => log.Editor == editor)
                    .OrderByDescending(log => log.EditDate);

                // Get total count
                int totalCount = await query.CountAsync();
                int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                // Apply pagination
                var pagedLogs = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Map to DTOs
                var logEntries = pagedLogs.Select(log => new LogEntryDTO
                {
                    Id = log.Id,
                    Model = log.Model,
                    TableName = log.TblName,
                    TableNameArabic = log.TblNameA,
                    RecordId = log.RecordId,
                    OperationType = log.OperationType,
                    Editor = log.Editor,
                    EditDate = log.EditDate,
                    IpAddress = log.Ipadress,
                    AccountUnitId = log.AccountUnitId
                }).ToList();

                var response = new
                {
                    Editor = editor,
                    Logs = logEntries,
                    TotalCount = totalCount,
                    TotalPages = totalPages,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                return new BaseResponseDTOs(response, 200, null);
            }
            catch (Exception ex)
            {
                return new BaseResponseDTOs(null, 500, $"Error retrieving user activity logs: {ex.Message}");
            }
        }

        /// <summary>
        /// Parses record data from the format key=value#key2=value2... to a dictionary
        /// </summary>
        /// <param name="recordData">Record data string</param>
        /// <returns>Dictionary of parsed key-value pairs</returns>
        private Dictionary<string, string> ParseRecordData(string? recordData)
        {
            var result = new Dictionary<string, string>();

            if (string.IsNullOrEmpty(recordData))
                return result;

            // Split the record data by # character (field separator)
            var fields = recordData.Split('#');
            
            foreach (var field in fields)
            {
                // Split each field by = character (key-value separator)
                var parts = field.Split(new[] { '=' }, 2); // Split into max 2 parts (key and value)
                
                if (parts.Length == 2)
                {
                    string key = parts[0];
                    string value = parts[1];
                    
                    // Add to dictionary, handling duplicate keys
                    if (!result.ContainsKey(key))
                    {
                        result.Add(key, value);
                    }
                    else
                    {
                        // If key already exists, append the new value
                        result[key] += ", " + value;
                    }
                }
            }
            
            return result;
        }

        /// <summary>
        /// Calculates changes between two document states
        /// </summary>
        /// <param name="oldState">The previous state</param>
        /// <param name="newState">The new state</param>
        /// <returns>Object containing added, modified, and removed fields</returns>
        private object CalculateChanges(Dictionary<string, string> oldState, Dictionary<string, string> newState)
        {
            var added = new Dictionary<string, string>();
            var modified = new Dictionary<string, string>();
            var removed = new Dictionary<string, string>();

            // Find added and modified fields
            foreach (var pair in newState)
            {
                if (!oldState.ContainsKey(pair.Key))
                {
                    // Field was added
                    added.Add(pair.Key, pair.Value);
                }
                else if (oldState[pair.Key] != pair.Value)
                {
                    // Field was modified
                    modified.Add(pair.Key, $"{oldState[pair.Key]} ? {pair.Value}");
                }
            }

            // Find removed fields
            foreach (var pair in oldState)
            {
                if (!newState.ContainsKey(pair.Key))
                {
                    // Field was removed
                    removed.Add(pair.Key, pair.Value);
                }
            }

            return new
            {
                Added = added,
                Modified = modified,
                Removed = removed,
                HasChanges = added.Count > 0 || modified.Count > 0 || removed.Count > 0
            };
        }
    }
}
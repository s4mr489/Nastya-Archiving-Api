using System;

namespace Nastya_Archiving_project.Models.DTOs.Logs
{
    /// <summary>
    /// Data Transfer Object for log entries
    /// </summary>
    public class LogEntryDTO
    {
        /// <summary>
        /// The log entry ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Name of the model/module being modified
        /// </summary>
        public string? Model { get; set; }

        /// <summary>
        /// Name of the table being modified
        /// </summary>
        public string? TableName { get; set; }

        /// <summary>
        /// Alternative name of the table
        /// </summary>
        public string? TableNameArabic { get; set; }

        /// <summary>
        /// ID of the record being modified
        /// </summary>
        public string? RecordId { get; set; }

        /// <summary>
        /// Type of operation performed (Add, Update, Delete, etc.)
        /// </summary>
        public string? OperationType { get; set; }

        /// <summary>
        /// Name of the user who performed the operation
        /// </summary>
        public string? Editor { get; set; }

        /// <summary>
        /// Date and time when the operation was performed
        /// </summary>
        public DateTime? EditDate { get; set; }

        /// <summary>
        /// IP address of the user who performed the operation
        /// </summary>
        public string? IpAddress { get; set; }

        /// <summary>
        /// Account unit ID associated with the operation
        /// </summary>
        public int? AccountUnitId { get; set; }
    }

    /// <summary>
    /// Detailed Data Transfer Object that includes parsed record data
    /// </summary>
    public class LogEntryDetailedDTO : LogEntryDTO
    {
        /// <summary>
        /// Record data parsed into key-value pairs
        /// </summary>
        public Dictionary<string, string> ParsedRecordData { get; set; } = new Dictionary<string, string>();
        
        /// <summary>
        /// Raw record data as stored in the database
        /// </summary>
        public string? RawRecordData { get; set; }
    }

    /// <summary>
    /// Request parameters for filtering logs
    /// </summary>
    public class LogsFilterDTO
    {
        /// <summary>
        /// Filter logs by operation type (Add, Update, Delete, etc.)
        /// </summary>
        public string? OperationType { get; set; }
        
        /// <summary>
        /// Filter logs by model/module
        /// </summary>
        public string? Model { get; set; }
        
        /// <summary>
        /// Filter logs by table name
        /// </summary>
        public string? TableName { get; set; }
        
        /// <summary>
        /// Filter logs by record ID
        /// </summary>
        public string? RecordId { get; set; }
        
        /// <summary>
        /// Filter logs by editor (user who performed the operation)
        /// </summary>
        public string? Editor { get; set; }
        
        /// <summary>
        /// Filter logs by start date
        /// </summary>
        public DateTime? StartDate { get; set; }
        
        /// <summary>
        /// Filter logs by end date
        /// </summary>
        public DateTime? EndDate { get; set; }
        
        /// <summary>
        /// Page number for pagination (1-based)
        /// </summary>
        public int PageNumber { get; set; } = 1;
        
        /// <summary>
        /// Number of items per page
        /// </summary>
        public int PageSize { get; set; } = 20;
    }
}
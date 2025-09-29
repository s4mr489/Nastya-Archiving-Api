namespace Nastya_Archiving_project.Models.DTOs.TextExtraction
{
    /// <summary>
    /// Result of processing multiple documents in batch mode
    /// </summary>
    public class BatchProcessingResult
    {
        /// <summary>
        /// Total number of documents processed
        /// </summary>
        public int TotalDocumentsProcessed { get; set; }
        
        /// <summary>
        /// Number of documents successfully processed
        /// </summary>
        public int SuccessfulDocuments { get; set; }
        
        /// <summary>
        /// Number of documents that failed processing
        /// </summary>
        public int FailedDocuments { get; set; }
        
        /// <summary>
        /// Total characters of text extracted
        /// </summary>
        public long TotalTextExtracted { get; set; }
        
        /// <summary>
        /// Time taken to process all documents in milliseconds
        /// </summary>
        public long ProcessingTimeMs { get; set; }
        
        /// <summary>
        /// Error message if batch processing failed
        /// </summary>
        public string Error { get; set; }
    }
}
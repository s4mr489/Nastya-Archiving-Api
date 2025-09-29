namespace Nastya_Archiving_project.Models.DTOs.TextExtraction
{
    /// <summary>
    /// Result of extracting text from a document by its reference number
    /// </summary>
    public class DocumentTextExtractionResult
    {
        /// <summary>
        /// The reference number of the document
        /// </summary>
        public string ReferenceNo { get; set; }
        
        /// <summary>
        /// Whether the extraction operation was successful
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// The extracted text content
        /// </summary>
        public string ExtractedText { get; set; }
        
        /// <summary>
        /// Length of the extracted text
        /// </summary>
        public int TextLength { get; set; }
        
        /// <summary>
        /// Error message if the extraction failed
        /// </summary>
        public string ErrorMessage { get; set; }
    }
}
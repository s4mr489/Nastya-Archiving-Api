using System;

namespace Nastya_Archiving_project.Models.DTOs.TextExtraction
{
    /// <summary>
    /// Represents the result of a text extraction operation from a PDF file
    /// </summary>
    public class TextExtractionResult
    {
        /// <summary>
        /// The extracted text content
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Indicates whether the text is right-to-left (e.g., Arabic)
        /// </summary>
        public bool IsRightToLeft { get; set; }

        /// <summary>
        /// The method used to extract the text (e.g., "text", "ocr", "python")
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Length of the extracted text
        /// </summary>
        public int TextLength { get; set; }

        /// <summary>
        /// Error message if the extraction failed
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// Information about the Python environment used for extraction
        /// </summary>
        public PythonEnvironmentInfo PythonInfo { get; set; }
    }
}
using Microsoft.AspNetCore.Http;
using Nastya_Archiving_project.Models.DTOs.TextExtraction;
using System.Threading.Tasks;

namespace Nastya_Archiving_project.Services.textExtraction
{
    /// <summary>
    /// Interface for text extraction services to handle PDF processing
    /// </summary>
    public interface ITextExtractionServices
    {
        /// <summary>
        /// Process multiple documents with no extracted text in batch mode
        /// </summary>
        /// <returns>Result of the batch processing operation</returns>
        Task<BatchProcessingResult> ProcessDocumentsWithNoTextAsync();

        /// <summary>
        /// Extract text from document by reference number and update the WordsTosearch field
        /// </summary>
        /// <param name="referenceNo">The document reference number</param>
        /// <returns>Extraction result with status and extracted text</returns>
        Task<DocumentTextExtractionResult> ExtractAndSaveDocumentTextByReferenceAsync(string referenceNo);

        /// <summary>
        /// Extract text from a text-based PDF file
        /// </summary>
        /// <param name="pdfPath">Path to the PDF file</param>
        /// <returns>Extracted text</returns>
        string ExtractTextFromTextPdf(string pdfPath);

        /// <summary>
        /// Extract text using OCR for image-based PDFs
        /// </summary>
        /// <param name="pdfPath">Path to the PDF file</param>
        /// <returns>Extracted text</returns>
        string ExtractTextUsingOcr(string pdfPath);

        /// <summary>
        /// Check if a PDF is text-based (has extractable text) or image-based
        /// </summary>
        /// <param name="pdfPath">Path to the PDF file</param>
        /// <returns>True if the PDF contains extractable text</returns>
        bool IsTextBasedPdf(string pdfPath);

        /// <summary>
        /// Process Arabic text to ensure correct alignment and rendering
        /// </summary>
        /// <param name="text">Raw text to process</param>
        /// <returns>Processed text optimized for Arabic display</returns>
        string ProcessArabicText(string text);

        /// <summary>
        /// Extract text from a PDF file using Python script
        /// </summary>
        /// <param name="pdfPath">Path to the PDF file</param>
        /// <returns>Extracted text</returns>
        string ExtractTextWithPython(string pdfPath);

        /// <summary>
        /// Extract text from a PDF file based on its type (text or image)
        /// </summary>
        /// <param name="pdfFile">PDF file from HTTP request</param>
        /// <returns>Extracted and processed text along with metadata</returns>
        Task<TextExtractionResult> ExtractTextFromPdfAsync(IFormFile pdfFile);

        /// <summary>
        /// Extract text from a PDF file using Python script
        /// </summary>
        /// <param name="pdfFile">PDF file from HTTP request</param>
        /// <returns>Extracted and processed text along with metadata</returns>
        Task<TextExtractionResult> ExtractTextWithPythonAsync(IFormFile pdfFile);

        /// <summary>
        /// Check the Python environment for required packages
        /// </summary>
        /// <returns>Information about the Python environment</returns>
        PythonEnvironmentInfo CheckPythonEnvironment();

        /// <summary>
        /// Install required Python packages
        /// </summary>
        /// <returns>Results of the installation attempts</returns>
        PythonPackageInstallationResult InstallPythonPackages();
    }
}
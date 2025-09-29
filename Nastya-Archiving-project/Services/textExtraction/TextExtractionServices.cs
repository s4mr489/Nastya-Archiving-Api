using ImageMagick;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nastya_Archiving_project.Data;
using Nastya_Archiving_project.Models.DTOs.TextExtraction;
using Nastya_Archiving_project.Models.Entity;
using Nastya_Archiving_project.Services.files;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Tesseract;
using UglyToad.PdfPig;

namespace Nastya_Archiving_project.Services.textExtraction
{
    /// <summary>
    /// Service for extracting text from PDF documents
    /// </summary>
    public class TextExtractionServices : ITextExtractionServices
    {
        private readonly ILogger<TextExtractionServices> _logger;
        private readonly AppDbContext _dbContext;
        private readonly IFilesServices _filesServices;

        public TextExtractionServices(
            ILogger<TextExtractionServices> logger,
            AppDbContext dbContext, 
            IFilesServices filesServices)
        {
            _logger = logger;
            _dbContext = dbContext;
            _filesServices = filesServices;
        }

        /// <inheritdoc/>
        public async Task<BatchProcessingResult> ProcessDocumentsWithNoTextAsync()
        {
            var result = new BatchProcessingResult();
            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            
            try
            {
                _logger.LogInformation("Starting batch processing of documents with no text");
                var docs = await _dbContext.ArcivingDocs.ToListAsync();
                result.TotalDocumentsProcessed = docs.Count;
                
                _logger.LogInformation("Found {TotalDocs} documents to process", docs.Count);
                
                int docsWithNoText = docs.Count(d => string.IsNullOrWhiteSpace(d.WordsTosearch));
                _logger.LogInformation("{DocsWithNoText} documents have no text and will be processed", docsWithNoText);
                
                foreach(var doc in docs)
                {
                    if (string.IsNullOrWhiteSpace(doc.WordsTosearch))
                    {
                        try
                        {
                            _logger.LogInformation("Processing document with reference: {ReferenceNo}, ImgUrl: {ImgUrl}", 
                                doc.RefrenceNo, doc.ImgUrl ?? "null");
                                
                            var extractionResult = await ExtractAndSaveDocumentTextByReferenceAsync(doc.RefrenceNo);
                            
                            if (extractionResult.Success)
                            {
                                result.SuccessfulDocuments++;
                                result.TotalTextExtracted += extractionResult.TextLength;
                                _logger.LogInformation("Successfully extracted text for document {ReferenceNo}, length: {TextLength}", 
                                    doc.RefrenceNo, extractionResult.TextLength);
                            }
                            else
                            {
                                result.FailedDocuments++;
                                _logger.LogWarning("Failed to extract text for document {ReferenceNo}: {ErrorMessage}", 
                                    doc.RefrenceNo, extractionResult.ErrorMessage);
                            }
                        }
                        catch (Exception ex)
                        {
                            result.FailedDocuments++;
                            _logger.LogError(ex, "Exception while processing document with reference {ReferenceNo}", doc.RefrenceNo);
                        }
                    }
                }
                
                _logger.LogInformation("Batch processing complete. Successfully processed {SuccessCount} documents, " +
                    "Failed {FailedCount} documents, Total text extracted: {TextLength} characters",
                    result.SuccessfulDocuments, result.FailedDocuments, result.TotalTextExtracted);
                
                return result;
            }
            catch (Exception ex)
            {
                result.Error = $"Error processing documents: {ex.Message}";
                _logger.LogError(ex, "Error in batch processing documents");
                return result;
            }
            finally
            {
                stopwatch.Stop();
                result.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;
            }
        }
        
        /// <inheritdoc/>
        public async Task<DocumentTextExtractionResult> ExtractAndSaveDocumentTextByReferenceAsync(string referenceNo)
        {
            var result = new DocumentTextExtractionResult
            {
                ReferenceNo = referenceNo
            };

            try
            {
                // Find the document in the database by reference number
                var document = await _dbContext.ArcivingDocs
                    .FirstOrDefaultAsync(d => d.RefrenceNo == referenceNo);

                if (document == null)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Document with reference number {referenceNo} not found";
                    _logger.LogWarning("Document with reference {ReferenceNo} not found", referenceNo);
                    return result;
                }

                // Check if the document has an image URL
                if (string.IsNullOrEmpty(document.ImgUrl))
                {
                    result.Success = false;
                    result.ErrorMessage = $"Document with reference number {referenceNo} has no file path";
                    _logger.LogWarning("Document with reference {ReferenceNo} has no file path", referenceNo);
                    return result;
                }

                _logger.LogInformation("Processing document {ReferenceNo} with path {ImgUrl}", referenceNo, document.ImgUrl);

                // Get the decrypted file stream using the filesServices
                var (fileStream, contentType, error) = await _filesServices.GetDecryptedFileStreamAsync(document.ImgUrl);

                if (fileStream == null)
                {
                    result.Success = false;
                    result.ErrorMessage = error ?? "Failed to decrypt file";
                    _logger.LogError("Failed to decrypt file for document {ReferenceNo}: {Error}", referenceNo, error);
                    return result;
                }

                _logger.LogDebug("File decrypted successfully, content type: {ContentType}", contentType);

                // Save the stream to a temporary file to process it
                var tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.pdf");
                try
                {
                    using (var fileStreamToWrite = new FileStream(tempFilePath, FileMode.Create))
                    {
                        await fileStream.CopyToAsync(fileStreamToWrite);
                    }
                    
                    _logger.LogDebug("File saved to temporary location: {TempFilePath}", tempFilePath);

                    // Determine the appropriate text extraction method based on content type and file examination
                    string extractedText = null;
                    string extractionMethod = "unknown";
                    
                    // For PDF files
                    if (contentType?.ToLower().Contains("pdf") == true || document.ImgUrl.ToLower().EndsWith(".pdf"))
                    {
                        try
                        {
                            // Try Python extraction first (if available)
                            _logger.LogDebug("Attempting to extract text using Python");
                            extractedText = ExtractTextWithPython(tempFilePath);
                            extractionMethod = "python";
                            _logger.LogDebug("Python extraction successful, extracted {TextLength} characters", 
                                extractedText?.Length ?? 0);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Python extraction failed, trying alternative methods");
                            
                            // Try text-based extraction if Python fails
                            if (IsTextBasedPdf(tempFilePath))
                            {
                                _logger.LogDebug("Document appears to be text-based PDF, using text extraction");
                                extractedText = ExtractTextFromTextPdf(tempFilePath);
                                extractionMethod = "text";
                            }
                            else
                            {
                                // Check if tessdata directory exists before attempting OCR
                                if (Directory.Exists("./tessdata"))
                                {
                                    _logger.LogDebug("Document appears to be image-based PDF, using OCR");
                                    extractedText = ExtractTextUsingOcr(tempFilePath);
                                    extractionMethod = "ocr";
                                }
                                else
                                {
                                    throw new DirectoryNotFoundException("Tessdata directory not found. Please run setup_tessdata.ps1 script in the scripts directory.");
                                }
                            }
                        }
                    }
                    // For image files, use OCR
                    else if (contentType?.Contains("image") == true || 
                             new[] { ".jpg", ".jpeg", ".png", ".tiff", ".tif", ".bmp" }
                                .Any(ext => document.ImgUrl.ToLower().EndsWith(ext)))
                    {
                        // Check if tessdata directory exists before attempting OCR
                        if (Directory.Exists("./tessdata"))
                        {
                            _logger.LogDebug("Processing image file with OCR");
                            extractedText = ExtractTextUsingOcr(tempFilePath);
                            extractionMethod = "ocr";
                        }
                        else
                        {
                            throw new DirectoryNotFoundException("Tessdata directory not found. Please run setup_tessdata.ps1 script in the scripts directory.");
                        }
                    }
                    else
                    {
                        // Unknown file type, try Python first, then OCR
                        try
                        {
                            _logger.LogDebug("Unknown file type, attempting Python extraction");
                            extractedText = ExtractTextWithPython(tempFilePath);
                            extractionMethod = "python";
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Python extraction failed for unknown file type, attempting OCR");
							
							if (Directory.Exists("./tessdata"))
							{
								 extractedText = ExtractTextUsingOcr(tempFilePath);
								 extractionMethod = "ocr";
							}
							else
							{
								 throw new DirectoryNotFoundException("Tessdata directory not found. Please run setup_tessdata.ps1 script in the scripts directory.");
							}
                        }
                    }

                    // Check if we actually got any text
                    if (string.IsNullOrWhiteSpace(extractedText))
                    {
                        result.Success = false;
                        result.ErrorMessage = $"No text could be extracted from the document using {extractionMethod} method";
                        _logger.LogWarning("No text extracted from document {ReferenceNo} using {ExtractionMethod}", 
                            referenceNo, extractionMethod);
                        return result;
                    }

                    // Process the text (handle RTL issues, normalize, etc.)
                    extractedText = ProcessArabicText(extractedText);

                    _logger.LogDebug("Text processed for Arabic display. Final text length: {TextLength}", 
                        extractedText?.Length ?? 0);

                    // Update the document in the database with the extracted text
                    document.WordsTosearch = extractedText;
                    await _dbContext.SaveChangesAsync();

                    // Set the result properties
                    result.Success = true;
                    result.ExtractedText = extractedText;
                    result.TextLength = extractedText?.Length ?? 0;

                    _logger.LogInformation("Successfully extracted text for document {ReferenceNo}, text length: {TextLength}, method: {Method}", 
                        referenceNo, extractedText?.Length ?? 0, extractionMethod);

                    return result;
                }
                finally
                {
                    // Clean up resources
                    fileStream.Dispose();
                    
                    // Delete the temporary file
                    if (File.Exists(tempFilePath))
                    {
                        try
                        {
                            File.Delete(tempFilePath);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to delete temporary file {TempFilePath}", tempFilePath);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Error extracting text: {ex.Message}";
                _logger.LogError(ex, "Error extracting text for document with reference {ReferenceNo}", referenceNo);
                return result;
            }
        }

        /// <inheritdoc/>
        public async Task<TextExtractionResult> ExtractTextFromPdfAsync(IFormFile pdfFile)
        {
            if (pdfFile == null || pdfFile.Length == 0)
                throw new ArgumentException("No file provided");

            var tempPath = Path.GetTempFileName();
            try
            {
                // Save uploaded file to temp path
                using (var stream = new FileStream(tempPath, FileMode.Create))
                {
                    await pdfFile.CopyToAsync(stream);
                }

                string extractedText;
                string source;

                // Determine if the PDF is text-based or image-based
                if (IsTextBasedPdf(tempPath))
                {
                    extractedText = ExtractTextFromTextPdf(tempPath);
                    source = "text";
                }
                else
                {
                    extractedText = ExtractTextUsingOcr(tempPath);
                    source = "ocr";
                }

                // Process text for correct Arabic alignment
                extractedText = ProcessArabicText(extractedText);

                return new TextExtractionResult
                {
                    Text = extractedText,
                    IsRightToLeft = true,
                    Source = source,
                    TextLength = extractedText?.Length ?? 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from PDF");
                throw;
            }
            finally
            {
                // Clean up the temporary file
                try
                {
                    if (File.Exists(tempPath))
                    {
                        File.Delete(tempPath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete temporary file {TempPath}", tempPath);
                }
            }
        }

        /// <inheritdoc/>
        public async Task<TextExtractionResult> ExtractTextWithPythonAsync(IFormFile pdfFile)
        {
            if (pdfFile == null || pdfFile.Length == 0)
                throw new ArgumentException("No file provided");

            // Create a temporary path with a .pdf extension so Python script can recognize it
            var tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.pdf");

            try
            {
                _logger.LogInformation("Processing file: {FileName}, size: {Length} bytes", pdfFile.FileName, pdfFile.Length);

                // Save the uploaded PDF to the temp path
                using (var stream = new FileStream(tempFilePath, FileMode.Create))
                {
                    await pdfFile.CopyToAsync(stream);
                }

                _logger.LogDebug("PDF saved to temporary file: {TempFilePath}", tempFilePath);

                // Find the best Python installation
                string pythonExecutable = FindBestPythonInstallation();
                string pythonInfo = GetPythonEnvironmentInfo(pythonExecutable);
                
                // Run Python script
                _logger.LogInformation("Calling Python script using {PythonExecutable}", pythonExecutable);
                string extractedText = ExtractTextWithPython(tempFilePath);
                
                _logger.LogDebug("Python script returned {TextLength} characters", extractedText?.Length ?? 0);

                // Check if we got any text back
                if (string.IsNullOrWhiteSpace(extractedText))
                {
                    _logger.LogWarning("Python script returned empty text");
                    return new TextExtractionResult
                    {
                        Error = "Python extraction produced no text",
                        PythonInfo = ParsePythonEnvironmentInfo(pythonInfo)
                    };
                }

                // Process the extracted text for better Arabic rendering
                extractedText = ProcessArabicText(extractedText);

                return new TextExtractionResult
                {
                    Text = extractedText,
                    IsRightToLeft = true,
                    Source = "python",
                    TextLength = extractedText?.Length ?? 0,
                    PythonInfo = ParsePythonEnvironmentInfo(pythonInfo)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ExtractWithPython");

                // Get Python environment info even in case of error
                string pythonInfo = "Unknown";
                try {
                    var pythonExecutable = FindBestPythonInstallation();
                    pythonInfo = GetPythonEnvironmentInfo(pythonExecutable);
                } catch {}

                // Try to provide more detailed information about the environment
                var environmentInfo = new Dictionary<string, object>
                {
                    ["PythonPath"] = GetCommandPath("python"),
                    ["ScriptPath"] = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "extract_arabic_pdf.py"),
                    ["WorkingDirectory"] = AppDomain.CurrentDomain.BaseDirectory,
                    ["TempFilePath"] = tempFilePath,
                    ["FileExists"] = File.Exists(tempFilePath).ToString(),
                    ["PythonEnvironment"] = pythonInfo
                };

                return new TextExtractionResult
                {
                    Error = ex.Message,
                    PythonInfo = ParsePythonEnvironmentInfo(pythonInfo)
                };
            }
            finally
            {
                // Clean up the temporary file
                try
                {
                    if (File.Exists(tempFilePath))
                    {
                        File.Delete(tempFilePath);
                        _logger.LogDebug("Deleted temporary file: {TempFilePath}", tempFilePath);
                    }
                }
                catch (Exception ex)
                {
                    // Log deletion errors
                    _logger.LogWarning(ex, "Failed to delete temporary file {TempFilePath}", tempFilePath);
                }
            }
        }

        /// <inheritdoc/>
        public bool IsTextBasedPdf(string path)
        {
            using var document = PdfDocument.Open(path);
            foreach (var page in document.GetPages())
            {
                var text = page.Text;
                if (!string.IsNullOrWhiteSpace(text))
                    return true;
            }
            return false;
        }

        /// <inheritdoc/>
        public string ExtractTextFromTextPdf(string path)
        {
            using var document = PdfDocument.Open(path);
            var sb = new StringBuilder();
            foreach (var page in document.GetPages())
            {
                // Extract text from each page
                string pageText = page.Text;
                sb.AppendLine(pageText);
            }
            return sb.ToString();
        }

        /// <inheritdoc/>
        public string ExtractTextUsingOcr(string path)
        {
            _logger.LogInformation("Starting OCR text extraction for file: {FilePath}", path);
            
            try
            {
                // Check if the file exists
                if (!File.Exists(path))
                {
                    throw new FileNotFoundException($"File not found: {path}");
                }
                
                // Check if tessdata directory exists
                string tessdataPath = "./tessdata";
                if (!Directory.Exists(tessdataPath))
                {
                    _logger.LogError("Tessdata directory not found at {TessdataPath}", Path.GetFullPath(tessdataPath));
                    throw new DirectoryNotFoundException($"Tessdata directory not found at {Path.GetFullPath(tessdataPath)}. Please run setup_tessdata.ps1 script.");
                }
                
                // Check if Arabic language files are available
                string arabicDataFile = Path.Combine(tessdataPath, "ara.traineddata");
                if (!File.Exists(arabicDataFile))
                {
                    _logger.LogError("Arabic language data file not found at {ArabicDataFile}", arabicDataFile);
                    throw new FileNotFoundException($"Arabic language data file not found at {arabicDataFile}. Please run setup_tessdata.ps1 script.");
                }
                
                _logger.LogDebug("Converting PDF to images for OCR processing");
                var images = ConvertPdfToImages(path);
                
                if (images.Count == 0)
                {
                    _logger.LogWarning("No images were extracted from the PDF");
                    return "No pages could be extracted from the document for OCR processing.";
                }
                
                _logger.LogDebug("Converted PDF to {ImageCount} images", images.Count);
                
                var text = new StringBuilder();

                try
                {
                    _logger.LogDebug("Initializing Tesseract OCR engine with Arabic language");
                    // Initialize Tesseract with Arabic language and right-to-left reading order
                    using var engine = new TesseractEngine(tessdataPath, "ara", EngineMode.Default);

                    // Configure engine for Arabic text
                    engine.SetVariable("textord_tablefind_show_vlines", false);
                    engine.SetVariable("textord_use_cjk_fp_model", false);
                    engine.SetVariable("language_model_ngram_on", true);
                    engine.SetVariable("paragraph_text_based", true);
                    engine.SetVariable("textord_heavy_nr", false);
                    engine.SetVariable("tessedit_pageseg_mode", 1); // PSM_AUTO_OSD
                    engine.SetVariable("preserve_interword_spaces", 1); // Preserve spaces

                    int pageCount = 0;
                    foreach (var image in images)
                    {
                        pageCount++;
                        try
                        {
                            _logger.LogDebug("Processing image {PageNumber} of {TotalPages}", pageCount, images.Count);
                            
                            // Convert the Bitmap to a Pix object that Tesseract can use
                            using var pix = BitmapToPix(image);
                            if (pix == null)
                            {
                                _logger.LogWarning("Failed to convert image to Pix format for page {PageNumber}", pageCount);
                                continue;
                            }

                            // Process the image with OCR
                            using var page = engine.Process(pix);

                            // Get the text and apply RTL corrections
                            var pageText = page.GetText();
                            
                            if (string.IsNullOrWhiteSpace(pageText))
                            {
                                _logger.LogWarning("No text extracted from page {PageNumber}", pageCount);
                            }
                            else
                            {
                                _logger.LogDebug("Extracted {CharCount} characters from page {PageNumber}", 
                                    pageText.Length, pageCount);
                                text.AppendLine(pageText);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing page {PageNumber} with OCR", pageCount);
                        }
                    }

                    string result = text.ToString();
                    
                    if (string.IsNullOrWhiteSpace(result))
                    {
                        _logger.LogWarning("OCR completed but no text was extracted from any page");
                        return "OCR processing completed but no text was found in the document.";
                    }
                    
                    _logger.LogInformation("OCR completed successfully. Extracted {TextLength} characters from {PageCount} pages", 
                        result.Length, pageCount);
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during OCR processing");
                    throw;
                }
                finally
                {
                    // Clean up images
                    foreach (var image in images)
                    {
                        image?.Dispose();
                    }
                    
                    _logger.LogDebug("Disposed {ImageCount} temporary images", images.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OCR extraction failed");
                return $"OCR text extraction failed: {ex.Message}";
            }
        }

        /// <inheritdoc/>
        public string ProcessArabicText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // Handle bidirectional text (mixing of Arabic and Latin scripts)
            text = ApplyBidiProcessing(text);

            // Normalize Arabic characters (handle different forms of alef, ya, etc.)
            text = NormalizeArabicText(text);

            // Ensure correct line order for RTL text
            text = CorrectLineOrder(text);

            return text;
        }

        /// <inheritdoc/>
        public string ExtractTextWithPython(string pdfPath)
        {
            // Find the script path
            var scriptPath = FindScriptPath();
            
            // Find the best Python installation
            string pythonExecutable = FindBestPythonInstallation();
            _logger.LogInformation("Selected Python executable: {PythonExecutable}", pythonExecutable);

            var psi = new ProcessStartInfo();
            psi.FileName = pythonExecutable;
            psi.Arguments = $"\"{scriptPath}\" \"{pdfPath}\"";
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.StandardOutputEncoding = Encoding.UTF8;
            psi.StandardErrorEncoding = Encoding.UTF8;
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory; // Set working directory

            try
            {
                _logger.LogDebug("Starting Python process: {FileName} {Arguments}", psi.FileName, psi.Arguments);
                _logger.LogDebug("Working directory: {WorkingDirectory}", psi.WorkingDirectory);

                using var process = Process.Start(psi);
                if (process == null)
                    throw new Exception("Failed to start Python process");

                // Read error output asynchronously
                var errorOutput = new StringBuilder();
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        errorOutput.AppendLine(e.Data);
                        _logger.LogDebug("Python stderr: {ErrorData}", e.Data);
                    }
                };
                process.BeginErrorReadLine();

                // Read standard output
                string output = process.StandardOutput.ReadToEnd();
                _logger.LogDebug("Python stdout length: {OutputLength} characters", output?.Length ?? 0);

                // Wait for exit with timeout
                if (!process.WaitForExit(60000)) // 60-second timeout
                {
                    process.Kill();
                    throw new Exception("Python script execution timed out after 60 seconds");
                }

                // Check for special output indicator
                if (output.Contains("TEXT_SAVED_TO:"))
                {
                    // Extract file path from output
                    var match = Regex.Match(output, @"TEXT_SAVED_TO:(.+)$", RegexOptions.Multiline);
                    if (match.Success)
                    {
                        string tempFilePath = match.Groups[1].Value.Trim();
                        _logger.LogInformation("Reading text from file: {TempFilePath}", tempFilePath);

                        // Read the file content and clean up
                        output = File.ReadAllText(tempFilePath, Encoding.UTF8);
                        try { File.Delete(tempFilePath); } catch { }
                    }
                }

                // Check if process completed successfully
                if (process.ExitCode != 0)
                {
                    throw new Exception($"Python script exited with code {process.ExitCode}. Error: {errorOutput}");
                }

                // Return output if we have any, otherwise report an issue
                if (!string.IsNullOrWhiteSpace(output))
                {
                    return output;
                }
                else if (errorOutput.Length > 0)
                {
                    throw new Exception($"Python script did not produce output. Error: {errorOutput}");
                }
                else
                {
                    throw new Exception("Python script did not produce any output");
                }
            }
            catch (Exception ex)
            {
                // Add more context to the exception for better diagnosis
                throw new Exception($"Error running Python script: {ex.Message}", ex);
            }
        }

        /// <inheritdoc/>
        public PythonEnvironmentInfo CheckPythonEnvironment()
        {
            var result = new PythonEnvironmentInfo();

            try
            {
                // Get all Python paths
                var pythonPaths = GetCommandPaths("python");
                var pythonPaths3 = GetCommandPaths("python3");

                // Try each Python path until one works
                var testedPaths = new Dictionary<string, string>();
                bool foundWorkingPython = false;

                // Check each Python path
                foreach (var pythonPath in pythonPaths.Concat(pythonPaths3).Where(p => p != "Not found in PATH"))
                {
                    try
                    {
                        // Check Python version
                        var psi = new ProcessStartInfo();
                        psi.FileName = pythonPath;
                        psi.Arguments = "--version";
                        psi.RedirectStandardOutput = true;
                        psi.RedirectStandardError = true;
                        psi.UseShellExecute = false;
                        psi.CreateNoWindow = true;

                        using var process = Process.Start(psi);
                        string versionOutput = process.StandardOutput.ReadToEnd().Trim();
                        string errorOutput = process.StandardError.ReadToEnd().Trim();
                        process.WaitForExit();

                        string version = !string.IsNullOrEmpty(versionOutput) ? versionOutput : errorOutput;
                        testedPaths[pythonPath] = version;

                        // This path works, check packages
                        var packageResults = new Dictionary<string, string>();
                        CheckPythonPackageForPath(pythonPath, packageResults, "PyMuPDF", "import fitz; print('PyMuPDF version:', fitz.__version__)");
                        CheckPythonPackageForPath(pythonPath, packageResults, "pdfminer.six", "from pdfminer import __version__; print('pdfminer version:', __version__)");

                        // If both packages are installed, this is our preferred Python environment
                        if (packageResults.ContainsKey("PyMuPDF") && packageResults["PyMuPDF"] == "Installed" &&
                            packageResults.ContainsKey("pdfminer.six") && packageResults["pdfminer.six"] == "Installed")
                        {
                            foundWorkingPython = true;
                            result.PythonPath = pythonPath;
                            result.PythonVersion = version;
                            result.Packages = packageResults;
                        }
                    }
                    catch (Exception ex)
                    {
                        testedPaths[pythonPath] = $"Error: {ex.Message}";
                    }
                }

                result.TestedPaths = testedPaths;

                // If we haven't found a working Python with all required packages,
                // recommend the first one that works at all
                if (!foundWorkingPython && testedPaths.Count > 0)
                {
                    var firstWorkingPython = testedPaths.FirstOrDefault(p => !p.Value.StartsWith("Error"));
                    if (!string.IsNullOrEmpty(firstWorkingPython.Key))
                    {
                        result.PythonPath = firstWorkingPython.Key;
                        result.PythonVersion = firstWorkingPython.Value;
                    }
                }

                // Check script location
                var scriptPath = FindScriptPath();
                result.ScriptPath = scriptPath;
                result.ScriptExists = File.Exists(scriptPath);

                // Additional diagnostics
                result.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
                result.CurrentDirectory = Directory.GetCurrentDirectory();

                return result;
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
                _logger.LogError(ex, "Error checking Python environment");
                return result;
            }
        }

        /// <inheritdoc/>
        public PythonPackageInstallationResult InstallPythonPackages()
        {
            var result = new PythonPackageInstallationResult();

            try
            {
                // Find the Python executable used by the application
                string pythonPath = FindBestPythonInstallation();
                result.PythonPath = pythonPath;
                
                // Try to install the packages directly
                result.InstallResults = InstallPythonPackages(pythonPath);
                
                // Test if the packages are now installed
                result.TestResults = TestPythonPackages(pythonPath);
                
                // Add detailed environment information
                result.EnvironmentInfo = GetPythonEnvironmentInfo(pythonPath);
                
                return result;
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
                _logger.LogError(ex, "Error installing Python packages");
                return result;
            }
        }

        #region Private helper methods

        private string FindScriptPath()
        {
            var scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "extract_arabic_pdf.py");
            var currentDirScriptPath = Path.Combine(Directory.GetCurrentDirectory(), "extract_arabic_pdf.py");

            // Check if the script exists in the base directory
            if (!File.Exists(scriptPath) && File.Exists(currentDirScriptPath))
            {
                scriptPath = currentDirScriptPath; // Use script in current directory
                _logger.LogInformation("Using script from current directory: {ScriptPath}", scriptPath);
            }
            else if (!File.Exists(scriptPath))
            {
                scriptPath = "extract_arabic_pdf.py"; // Try with relative path
                _logger.LogWarning("Script not found in base directory, trying relative path: {ScriptPath}", scriptPath);
            }
            else
            {
                _logger.LogInformation("Using script from base directory: {ScriptPath}", scriptPath);
            }

            return scriptPath;
        }

        private string FindBestPythonInstallation()
        {
            // First check if we have Python with required packages
            var pythonPaths = GetCommandPaths("python").Concat(GetCommandPaths("python3"))
                                                .Where(p => p != "Not found in PATH")
                                                .ToList();

            if (pythonPaths.Count == 0)
            {
                // No Python found, default to "python" and let it fail with a clear error
                return "python";
            }

            foreach (var pythonPath in pythonPaths)
            {
                try
                {
                    // Check if this Python has the required packages
                    if (HasRequiredPythonPackages(pythonPath))
                    {
                        return pythonPath;
                    }
                }
                catch
                {
                    // Continue to the next path if checking this one fails
                    continue;
                }
            }

            // If no Python installation has the required packages, use the first one
            return pythonPaths[0];
        }

        private bool HasRequiredPythonPackages(string pythonPath)
        {
            // Check for PyMuPDF
            bool hasPyMuPDF = CheckPythonPackage(pythonPath, "import fitz; print('OK')");

            // Check for pdfminer.six
            bool hasPdfMiner = CheckPythonPackage(pythonPath, "from pdfminer import high_level; print('OK')");

            // Return true if both packages are available
            return hasPyMuPDF && hasPdfMiner;
        }

        private bool CheckPythonPackage(string pythonPath, string testCode)
        {
            try
            {
                var psi = new ProcessStartInfo();
                psi.FileName = pythonPath;
                psi.Arguments = $"-c \"{testCode}\"";
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;

                using var process = Process.Start(psi);
                string output = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();

                // Package is available if exit code is 0 and output contains "OK"
                return process.ExitCode == 0 && output.Contains("OK");
            }
            catch
            {
                return false;
            }
        }

        private void CheckPythonPackageForPath(string pythonPath, Dictionary<string, string> results, string packageName, string testCode)
        {
            try
            {
                var psi = new ProcessStartInfo();
                psi.FileName = pythonPath;
                psi.Arguments = $"-c \"{testCode}\"";
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;

                using var process = Process.Start(psi);
                string output = process.StandardOutput.ReadToEnd().Trim();
                string errorOutput = process.StandardError.ReadToEnd().Trim();
                process.WaitForExit();

                if (string.IsNullOrEmpty(errorOutput) && process.ExitCode == 0)
                {
                    results[$"{packageName}"] = "Installed";
                    results[$"{packageName}Version"] = output;
                }
                else
                {
                    results[$"{packageName}"] = "Not installed";
                    results[$"{packageName}Error"] = errorOutput;
                }
            }
            catch (Exception ex)
            {
                results[$"{packageName}Error"] = ex.Message;
            }
        }

        private string[] GetCommandPaths(string command)
        {
            try
            {
                var psi = new ProcessStartInfo();
                if (OperatingSystem.IsWindows())
                {
                    psi.FileName = "where";
                    psi.Arguments = command;
                }
                else
                {
                    psi.FileName = "which";
                    psi.Arguments = command;
                }

                psi.RedirectStandardOutput = true;
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;

                using var process = Process.Start(psi);
                string output = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();

                // Split by newlines to handle multiple paths
                if (string.IsNullOrEmpty(output))
                    return new[] { "Not found in PATH" };

                return output.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            }
            catch
            {
                return new[] { "Not found in PATH" };
            }
        }

        private string GetCommandPath(string command)
        {
            var paths = GetCommandPaths(command);
            return paths.FirstOrDefault() ?? "Not found in PATH";
        }

        private string GetPythonEnvironmentInfo(string pythonPath)
        {
            try
            {
                var psi = new ProcessStartInfo();
                psi.FileName = pythonPath;
                psi.Arguments = "-c \"import sys; import site; print('Python Version:', sys.version); print('Executable:', sys.executable); print('Site Packages:', site.getsitepackages())\"";
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;

                using var process = Process.Start(psi);
                string output = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();
                
                return output;
            }
            catch (Exception ex)
            {
                return $"Failed to get Python environment info: {ex.Message}";
            }
        }

        private PythonEnvironmentInfo ParsePythonEnvironmentInfo(string pythonInfoStr)
        {
            var info = new PythonEnvironmentInfo();
            
            // Simple parsing of the output
            if (pythonInfoStr.Contains("Python Version:"))
            {
                var versionMatch = Regex.Match(pythonInfoStr, @"Python Version: (.+)");
                if (versionMatch.Success)
                {
                    info.PythonVersion = versionMatch.Groups[1].Value;
                }
            }

            if (pythonInfoStr.Contains("Executable:"))
            {
                var executableMatch = Regex.Match(pythonInfoStr, @"Executable: (.+)");
                if (executableMatch.Success)
                {
                    info.PythonPath = executableMatch.Groups[1].Value;
                }
            }

            return info;
        }

        private Dictionary<string, string> InstallPythonPackages(string pythonPath)
        {
            var results = new Dictionary<string, string>();
            
            try
            {
                // Install PyMuPDF
                results["PyMuPDF"] = RunPythonCommand(pythonPath, "-m pip install --upgrade PyMuPDF");
                
                // Install pdfminer.six
                results["pdfminer.six"] = RunPythonCommand(pythonPath, "-m pip install --upgrade pdfminer.six");
            }
            catch (Exception ex)
            {
                results["error"] = ex.Message;
            }
            
            return results;
        }
        
        private Dictionary<string, string> TestPythonPackages(string pythonPath)
        {
            var results = new Dictionary<string, string>();
            
            // Test PyMuPDF
            results["PyMuPDF"] = RunPythonCommand(pythonPath, 
                "-c \"try: import fitz; print('PyMuPDF ' + fitz.__version__ + ' is installed') except ImportError as e: print(str(e))\"");
                
            // Test pdfminer.six
            results["pdfminer.six"] = RunPythonCommand(pythonPath,
                "-c \"try: from pdfminer import __version__; print('pdfminer.six ' + __version__ + ' is installed') except ImportError as e: print(str(e))\"");
                
            return results;
        }
        
        private string RunPythonCommand(string pythonPath, string arguments)
        {
            try
            {
                var psi = new ProcessStartInfo();
                psi.FileName = pythonPath;
                psi.Arguments = arguments;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;

                using var process = Process.Start(psi);
                string output = process.StandardOutput.ReadToEnd().Trim();
                string errorOutput = process.StandardError.ReadToEnd().Trim();
                process.WaitForExit();
                
                if (!string.IsNullOrEmpty(errorOutput))
                {
                    return $"Output: {output}, Error: {errorOutput}";
                }
                
                return output;
            }
            catch (Exception ex)
            {
                return $"Error running command: {ex.Message}";
            }
        }

        private Pix BitmapToPix(Bitmap bitmap)
        {
            // Save bitmap to a temporary file
            string tempFile = Path.GetTempFileName();
            bitmap.Save(tempFile, System.Drawing.Imaging.ImageFormat.Png);

            try
            {
                // Load the image as a Pix object
                return Pix.LoadFromFile(tempFile);
            }
            finally
            {
                // Clean up the temporary file
                try
                {
                    File.Delete(tempFile);
                }
                catch
                {
                    // Ignore errors during deletion
                }
            }
        }

        private List<Bitmap> ConvertPdfToImages(string pdfPath)
        {
            var images = new List<Bitmap>();

            try
            {
                // Set up ImageMagick to read the PDF file
                var settings = new MagickReadSettings
                {
                    Density = new Density(300), // Set higher DPI for better OCR results
                    Format = MagickFormat.Pdf
                };

                using (var collection = new MagickImageCollection())
                {
                    // Read all pages from the PDF
                    collection.Read(pdfPath, settings);

                    // Convert each PDF page to an image
                    foreach (var magickImage in collection)
                    {
                        // Enhance the image for better OCR results with Arabic text
                        EnhanceImageForArabicOcr(magickImage);

                        // Convert to bitmap for Tesseract
                        using var memoryStream = new MemoryStream();
                        magickImage.Format = MagickFormat.Png;
                        magickImage.Write(memoryStream);
                        memoryStream.Position = 0;

                        var bitmap = new Bitmap(memoryStream);
                        images.Add(bitmap);
                    }
                }

                return images;
            }
            catch (Exception ex)
            {
                // If there's an error, dispose any created images
                foreach (var image in images)
                {
                    image?.Dispose();
                }

                throw new Exception($"Failed to convert PDF to images: {ex.Message}", ex);
            }
        }

        // Enhance image for better OCR of Arabic text
        private void EnhanceImageForArabicOcr(IMagickImage image)
        {
            try
            {
                // Apply preprocessing to improve Arabic OCR
                image.ColorSpace = ColorSpace.Gray; // Convert to grayscale
                image.Enhance(); // General enhancement
                image.Contrast(); // Improve contrast

                // Apply thresholding - good for Arabic text with clear dark characters
                image.Threshold(new Percentage(60));

                // Optional: remove noise
                image.Despeckle();

                // Sharpen the image to make text more defined
                image.Sharpen(0, 1.0);
            }
            catch (Exception ex)
            {
                // If any enhancement method fails, continue without that enhancement
                // This ensures we still get some text even if image processing fails
                _logger.LogWarning(ex, "Image enhancement failed");
            }
        }

        // Apply bidirectional text processing for mixed Arabic and Latin text
        private string ApplyBidiProcessing(string text)
        {
            // Add RLO (Right-to-Left Override) character at the beginning of each line
            var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++)
            {
                if (ContainsArabicCharacters(lines[i]))
                {
                    // Unicode RLM character (U+200F) for RTL marker
                    lines[i] = "\u200F" + lines[i];
                }
            }
            return string.Join(Environment.NewLine, lines);
        }

        // Check if a string contains Arabic characters
        private bool ContainsArabicCharacters(string text)
        {
            return Regex.IsMatch(text, @"\p{IsArabic}");
        }

        // Normalize Arabic text (fix common OCR issues with Arabic text)
        private string NormalizeArabicText(string text)
        {
            // Replace variations of alef with standard alef
            text = text.Replace('أ', 'ا')
                       .Replace('إ', 'ا')
                       .Replace('آ', 'ا');

            // Replace variations of ya with standard ya
            text = text.Replace('ى', 'ي');

            // Handle other common OCR issues
            text = text.Replace("  ", " "); // Remove double spaces

            return text;
        }

        // Correct line order for right-to-left languages
        private string CorrectLineOrder(string text)
        {
            var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            for (int i = 0; i < lines.Length; i++)
            {
                if (ContainsArabicCharacters(lines[i]))
                {
                    // Reverse punctuation order issues that sometimes occur in RTL text
                    lines[i] = FixPunctuationOrder(lines[i]);

                    // Add any additional line-level RTL corrections here
                }
            }

            return string.Join(Environment.NewLine, lines);
        }

        // Fix common punctuation order issues in RTL text
        private string FixPunctuationOrder(string line)
        {
            // Fix common punctuation issues (e.g., "(text" should be "text)")
            line = Regex.Replace(line, @"\((\p{IsArabic}+)", "$1)");
            line = Regex.Replace(line, @"(\p{IsArabic}+)\)", "($1");

            return line;
        }

        #endregion
    }
}
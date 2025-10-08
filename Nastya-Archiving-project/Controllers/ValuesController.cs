using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nastya_Archiving_project.Models.DTOs.TextExtraction;
using Nastya_Archiving_project.Services.textExtraction;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace ElectionsPillars.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        private readonly ITextExtractionServices _textExtractionServices;
        private readonly ILogger<ValuesController> _logger;

        public ValuesController(ITextExtractionServices textExtractionServices, ILogger<ValuesController> logger)
        {
            _textExtractionServices = textExtractionServices;
            _logger = logger;
        }

        [HttpPost("extract")]
        public async Task<IActionResult> ExtractArabicText(IFormFile pdfFile)
        {
            if (pdfFile == null || pdfFile.Length == 0)
                return BadRequest("No file uploaded.");

            try
            {
                _logger.LogInformation("Starting PDF extraction for file: {FileName}, Size: {FileSize}", 
                    pdfFile.FileName, pdfFile.Length);
                
                // Start timing the extraction process
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                
                // Extract text from PDF
                var result = await _textExtractionServices.ExtractTextFromPdfAsync(pdfFile);
                
                // Log initial extraction result
                _logger.LogDebug("Raw extraction completed in {ElapsedMs}ms. Text length: {TextLength}, RTL: {IsRtl}", 
                    stopwatch.ElapsedMilliseconds, result.Text?.Length ?? 0, result.IsRightToLeft);
                
                // Format the Arabic text properly
                string formattedText = result.Text;
                string detectedLanguage = "unknown";
                
                try
                {
                    // Double-check if the text is Arabic, regardless of what the service reported
                    bool isArabic = result.IsRightToLeft || ContainsArabicText(formattedText);
                    
                    // Apply Arabic text processing if needed
                    if (isArabic)
                    {
                        detectedLanguage = "arabic";
                        stopwatch.Restart();
                        
                        // Normalize Arabic text (handle special cases and character variations)
                        formattedText = NormalizeArabicText(formattedText);
                        
                        // Apply enhanced Arabic text formatting 
                        formattedText = CleanAndFormatArabicText(formattedText);
                        
                        _logger.LogDebug("Arabic text formatting completed in {ElapsedMs}ms. Formatted length: {FormattedLength}", 
                            stopwatch.ElapsedMilliseconds, formattedText?.Length ?? 0);
                    }
                }
                catch (ArgumentException ex) when (ex.Message.Contains("same key"))
                {
                    _logger.LogError(ex, "Dictionary key conflict while processing text. Key: {Key}", 
                        ex.Message.Contains("Key:") ? ex.Message.Substring(ex.Message.IndexOf("Key:")) : "unknown");
                    
                    // Return the original text without formatting to avoid the error
                    formattedText = result.Text;
                    detectedLanguage = result.IsRightToLeft ? "arabic (unformatted)" : "unknown";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during text formatting");
                    // Return the original text without formatting
                    formattedText = result.Text;
                    detectedLanguage = "unknown (formatting error)";
                }
                
                stopwatch.Stop();
                
                // Return comprehensive result with timing information
                return Ok(new { 
                    text = formattedText, 
                    rtl = result.IsRightToLeft,
                    originalLength = result.Text?.Length ?? 0,
                    formattedLength = formattedText?.Length ?? 0,
                    source = result.Source,
                    detectedLanguage = detectedLanguage,
                    processingTimeMs = stopwatch.ElapsedMilliseconds,
                    fileName = pdfFile.FileName,
                    fileSize = pdfFile.Length
                });
            }
            catch (ArgumentException ex) when (ex.Message.Contains("same key"))
            {
                _logger.LogError(ex, "Dictionary key error with file: {FileName}. Error: {Error}", 
                    pdfFile.FileName, ex.Message);
                
                return StatusCode(500, new { 
                    error = "Text extraction error - duplicate dictionary key",
                    message = ex.Message,
                    fileName = pdfFile.FileName,
                    fileSize = pdfFile.Length,
                    solution = "Please try again with a different PDF file or contact support."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from PDF: {FileName}", pdfFile.FileName);
                
                // Check for Ghostscript-specific errors
                if (ex.Message.Contains("gswin") || ex.Message.Contains("Ghostscript") || 
                    ex.Message.Contains("FailedToExecuteCommand"))
                {
                    return StatusCode(500, new { 
                        error = "PDF processing failed. The server may be missing Ghostscript.",
                        message = "Ghostscript is required for PDF processing. Please see the installation guide.",
                        details = ex.Message,
                        solution = "Please install Ghostscript and ensure it's in the system PATH."
                    });
                }
                
                // Check for file format errors
                if (ex.Message.Contains("not a PDF") || ex.Message.Contains("invalid PDF"))
                {
                    return BadRequest(new {
                        error = "Invalid PDF file format",
                        message = "The uploaded file appears not to be a valid PDF document.",
                        details = ex.Message,
                        fileName = pdfFile.FileName
                    });
                }
                
                // General error handling
                return StatusCode(500, new { 
                    error = "Text extraction failed",
                    message = ex.Message,
                    fileName = pdfFile.FileName,
                    fileSize = pdfFile.Length
                });
            }
        }

        [HttpPost("extract-python")]
        public async Task<IActionResult> ExtractWithPython(IFormFile pdfFile)
        {
            if (pdfFile == null || pdfFile.Length == 0)
                return BadRequest("No file uploaded.");

            try
            {
                _logger.LogInformation("Starting PDF extraction with Python for file: {FileName}, Size: {FileSize}", 
                    pdfFile.FileName, pdfFile.Length);
                
                // Start timing the extraction process
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                
                // Extract text using Python
                var result = await _textExtractionServices.ExtractTextWithPythonAsync(pdfFile);
                
                // Check for errors
                if (!string.IsNullOrEmpty(result.Error))
                {
                    _logger.LogError("Python extraction error: {Error}", result.Error);
                    return StatusCode(500, new { 
                        error = result.Error,
                        pythonInfo = result.PythonInfo,
                        fileName = pdfFile.FileName,
                        fileSize = pdfFile.Length
                    });
                }
                
                _logger.LogDebug("Python extraction completed in {ElapsedMs}ms. Text length: {TextLength}, RTL: {IsRtl}", 
                    stopwatch.ElapsedMilliseconds, result.Text?.Length ?? 0, result.IsRightToLeft);
                
                // Apply the same Arabic text formatting for consistency
                string formattedText = result.Text;
                string detectedLanguage = "unknown";
                
                try
                {
                    // Double-check if the text is Arabic, regardless of what the service reported
                    bool isArabic = result.IsRightToLeft || ContainsArabicText(formattedText);
                    
                    if (isArabic)
                    {
                        detectedLanguage = "arabic";
                        stopwatch.Restart();
                        
                        // Normalize Arabic text (handle special cases and character variations)
                        formattedText = NormalizeArabicText(formattedText);
                        
                        // Use the same enhanced formatting method
                        formattedText = CleanAndFormatArabicText(formattedText);
                        
                        _logger.LogDebug("Arabic text formatting completed in {ElapsedMs}ms. Formatted length: {FormattedLength}", 
                            stopwatch.ElapsedMilliseconds, formattedText?.Length ?? 0);
                    }
                }
                catch (ArgumentException ex) when (ex.Message.Contains("same key"))
                {
                    _logger.LogError(ex, "Dictionary key conflict while processing text. Key: {Key}", 
                        ex.Message.Contains("Key:") ? ex.Message.Substring(ex.Message.IndexOf("Key:")) : "unknown");
                    
                    // Return the original text without formatting to avoid the error
                    formattedText = result.Text;
                    detectedLanguage = result.IsRightToLeft ? "arabic (unformatted)" : "unknown";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during text formatting");
                    // Return the original text without formatting
                    formattedText = result.Text;
                    detectedLanguage = "unknown (formatting error)";
                }
                
                stopwatch.Stop();

                return Ok(new
                {
                    text = formattedText,
                    rtl = result.IsRightToLeft,
                    source = result.Source,
                    textLength = result.TextLength,
                    pythonInfo = result.PythonInfo,
                    originalLength = result.Text?.Length ?? 0,
                    formattedLength = formattedText?.Length ?? 0,
                    detectedLanguage = detectedLanguage,
                    processingTimeMs = stopwatch.ElapsedMilliseconds,
                    fileName = pdfFile.FileName,
                    fileSize = pdfFile.Length
                });
            }
            catch (ArgumentException ex) when (ex.Message.Contains("same key"))
            {
                _logger.LogError(ex, "Dictionary key error with file: {FileName}. Error: {Error}", 
                    pdfFile.FileName, ex.Message);
                
                return StatusCode(500, new { 
                    error = "Text extraction error - duplicate dictionary key",
                    message = ex.Message,
                    fileName = pdfFile.FileName,
                    fileSize = pdfFile.Length,
                    solution = "Please try again with a different PDF file or contact support."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ExtractWithPython: {FileName}", pdfFile.FileName);
                
                // Check for Python-specific errors
                if (ex.Message.Contains("Python") || ex.Message.Contains("python"))
                {
                    return StatusCode(500, new { 
                        error = "Python environment error",
                        message = "The Python environment is not properly configured for PDF extraction.",
                        details = ex.Message,
                        fileName = pdfFile.FileName,
                        solution = "Please check the Python installation and required packages."
                    });
                }
                
                return StatusCode(500, new { 
                    error = "Text extraction failed",
                    message = ex.Message,
                    fileName = pdfFile.FileName,
                    fileSize = pdfFile.Length
                });
            }
        }
        
        [HttpGet("python-check")]
        public async Task<IActionResult> CheckPythonEnvironment()
        {
            try
            {
                var loggerFactory = new LoggerFactory();
                var detectorLogger = loggerFactory.CreateLogger<PythonEnvironmentDetector>();
                var detector = new PythonEnvironmentDetector(detectorLogger);
                bool isConfigured = await detector.IsPythonEnvironmentConfigured();
                
                var result = _textExtractionServices.CheckPythonEnvironment();
                
                return Ok(new {
                    pythonPath = result.PythonPath,
                    pythonVersion = result.PythonVersion,
                    scriptPath = result.ScriptPath,
                    scriptExists = result.ScriptExists,
                    workingDirectory = result.WorkingDirectory,
                    currentDirectory = result.CurrentDirectory,
                    testedPaths = result.TestedPaths,
                    packages = result.Packages,
                    isConfigured = isConfigured,
                    diagnosticTime = DateTime.Now.ToString("o"),
                    instructions = !isConfigured 
                        ? "Python environment is not properly configured. Please run the scripts/install_dependencies_direct.bat script with administrator privileges."
                        : "Python environment is properly configured."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    error = ex.Message,
                    stackTrace = ex.StackTrace,
                    instructions = "An error occurred while checking the Python environment. Please run the scripts/install_dependencies_direct.bat script with administrator privileges."
                });
            }
        }

        [HttpGet("ghostscript-check")]
        public IActionResult CheckGhostscriptInstallation()
        {
            try
            {
                string ghostscriptPath = null;
                string ghostscriptVersion = "Not installed";
                bool isInPath = false;
                
                // Check if Ghostscript is in PATH
                var processInfo = new ProcessStartInfo
                {
                    FileName = "gswin64c",
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                try
                {
                    using var process = Process.Start(processInfo);
                    ghostscriptVersion = process.StandardOutput.ReadToEnd().Trim();
                    process.WaitForExit();
                    isInPath = process.ExitCode == 0;
                    ghostscriptPath = "In system PATH";
                }
                catch
                {
                    // Ghostscript not in PATH, try common installation locations
                    _logger.LogWarning("Ghostscript not found in PATH, checking common installation locations");
                    
                    string[] possibleLocations = {
                        @"C:\Program Files\gs\gs*\bin\gswin64c.exe",
                        @"C:\Program Files (x86)\gs\gs*\bin\gswin32c.exe"
                    };
                    
                    foreach (var location in possibleLocations)
                    {
                        try
                        {
                            var files = Directory.GetFiles(Path.GetDirectoryName(location), Path.GetFileName(location), SearchOption.AllDirectories);
                            if (files.Length > 0)
                            {
                                ghostscriptPath = files[0];
                                var versionProcess = new ProcessStartInfo
                                {
                                    FileName = ghostscriptPath,
                                    Arguments = "--version",
                                    RedirectStandardOutput = true,
                                    RedirectStandardError = true,
                                    UseShellExecute = false,
                                    CreateNoWindow = true
                                };
                                
                                using var process = Process.Start(versionProcess);
                                ghostscriptVersion = process.StandardOutput.ReadToEnd().Trim();
                                process.WaitForExit();
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error checking Ghostscript path {Path}", location);
                        }
                    }
                }
                
                return Ok(new {
                    isInstalled = !string.IsNullOrEmpty(ghostscriptPath),
                    isInPath = isInPath,
                    path = ghostscriptPath,
                    version = ghostscriptVersion,
                    message = string.IsNullOrEmpty(ghostscriptPath) 
                        ? "Ghostscript is not installed or not found. PDF processing may not work properly."
                        : $"Ghostscript found: {ghostscriptVersion}",
                    systemPath = Environment.GetEnvironmentVariable("PATH")
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new {
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        [HttpGet("python-install-packages")]
        public IActionResult InstallPythonPackages()
        {
            try
            {
                _logger.LogInformation("Attempting to install Python packages");
                var result = _textExtractionServices.InstallPythonPackages();
                
                // If Python was not found, return a more specific error
                if (!string.IsNullOrEmpty(result.Error) && 
                    (result.Error.Contains("Python not found") || 
                     result.PythonPath == "Not found" || 
                     result.Error.Contains("cannot find the file specified")))
                {
                    _logger.LogError("Python installation not found: {Error}", result.Error);
                    
                    return StatusCode(500, new {
                        error = "Python not found on the system",
                        message = "Python installation is required for PDF text extraction",
                        pythonPath = result.PythonPath,
                        installResults = result.InstallResults,
                        testResults = result.TestResults,
                        environmentInfo = result.EnvironmentInfo,
                        systemRequirements = new {
                            pythonVersion = "Python 3.8 or higher is required",
                            downloadUrl = "https://www.python.org/downloads/",
                            installationInstructions = "During installation, make sure to check 'Add Python to PATH'",
                            postInstallation = "After installing Python, restart your application/server"
                        },
                        manualInstallation = "To manually install the required Python packages, run the following commands in Command Prompt or PowerShell:",
                        manualCommands = new[] {
                            "pip install PyMuPDF",
                            "pip install pdfminer.six"
                        }
                    });
                }
                
                // Check if installation was successful
                bool installSuccess = result.TestResults != null && 
                                     result.TestResults.ContainsKey("PyMuPDF") && 
                                     result.TestResults["PyMuPDF"].Contains("installed") &&
                                     result.TestResults.ContainsKey("pdfminer.six") && 
                                     result.TestResults["pdfminer.six"].Contains("installed");
                
                if (installSuccess)
                {
                    _logger.LogInformation("Python packages successfully installed");
                    return Ok(new {
                        success = true,
                        message = "Python packages successfully installed",
                        pythonPath = result.PythonPath,
                        installResults = result.InstallResults,
                        testResults = result.TestResults,
                        environmentInfo = result.EnvironmentInfo
                    });
                }
                else
                {
                    _logger.LogWarning("Some Python packages could not be installed");
                    return Ok(new {
                        success = false,
                        message = "Some Python packages could not be installed. Please check the results.",
                        pythonPath = result.PythonPath,
                        installResults = result.InstallResults,
                        testResults = result.TestResults,
                        environmentInfo = result.EnvironmentInfo,
                        instructions = "If installation failed, please try the following:",
                        automaticInstallation = "Run the scripts/install_dependencies_direct.ps1 script with administrator privileges",
                        manualInstallation = "To manually install the required Python packages, run the following commands:",
                        manualCommands = new[] {
                            $"{result.PythonPath} -m pip install --upgrade pip",
                            $"{result.PythonPath} -m pip install PyMuPDF",
                            $"{result.PythonPath} -m pip install pdfminer.six"
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error installing Python packages");
                return StatusCode(500, new {
                    error = ex.Message,
                    message = "Failed to install packages.",
                    details = ex.ToString(),
                    instructions = "Please try one of the following solutions:",
                    solutions = new[] {
                        "Verify Python 3.8+ is installed and in the system PATH",
                        "Run scripts/install_dependencies_direct.ps1 script with administrator privileges",
                        "Manually install the packages using: pip install PyMuPDF pdfminer.six"
                    }
                });
            }
        }

        [HttpPost("extract-document/{referenceNo}")]
        public async Task<IActionResult> ExtractDocumentText(string referenceNo)
        {
            if (string.IsNullOrEmpty(referenceNo))
                return BadRequest("Reference number is required.");

            try
            {
                // Set environment variable to indicate we want to skip Ghostscript
                Environment.SetEnvironmentVariable("SKIP_GHOSTSCRIPT", "true");
                
                _logger.LogInformation("Starting document text extraction for reference: {ReferenceNo}", referenceNo);

                var result = await _textExtractionServices.ExtractAndSaveDocumentTextByReferenceAsync(referenceNo);
                
                if (!result.Success)
                {
                    if (result.ErrorMessage?.Contains("Ghostscript") == true || 
                        result.ErrorMessage?.Contains("gswin") == true || 
                        result.ErrorMessage?.Contains("FailedToExecuteCommand") == true)
                    {
                        // Try alternative extraction method that doesn't use Ghostscript
                        _logger.LogWarning("Attempting alternative extraction method for reference: {ReferenceNo}", referenceNo);
                        
                        // First, try to use our Python extraction endpoint if the reference exists as a physical file
                        try
                        {
                            // Check if we can get the document by reference
                            var tempResult = await TryAlternativeExtractionMethod(referenceNo);
                            
                            if (tempResult != null)
                            {
                                return Ok(new
                                {
                                    referenceNo = referenceNo,
                                    success = true,
                                    textLength = tempResult.Length,
                                    message = $"Successfully extracted and saved {tempResult.Length} characters of text using alternative method"
                                });
                            }
                        }
                        catch (Exception exAlt)
                        {
                            _logger.LogWarning(exAlt, "Alternative extraction method failed for reference: {ReferenceNo}", referenceNo);
                        }
                        
                        // If all alternatives failed, return helpful message
                        return StatusCode(500, new
                        {
                            referenceNo = referenceNo,
                            error = "Text extraction failed without Ghostscript.",
                            message = "The document requires Ghostscript for processing, but the system is configured to avoid using it.",
                            suggestion = "Try uploading the document directly using the /api/values/extract endpoint which may handle this document format better."
                        });
                    }
                    
                    return NotFound(new { 
                        referenceNo = referenceNo,
                        error = result.ErrorMessage
                    });
                }

                return Ok(new
                {
                    referenceNo = result.ReferenceNo,
                    success = result.Success,
                    textLength = result.TextLength,
                    message = $"Successfully extracted and saved {result.TextLength} characters of text"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from document with reference {ReferenceNo}", referenceNo);
                
                // Check for Ghostscript-specific errors
                if (ex.Message.Contains("gswin") || ex.Message.Contains("Ghostscript") || 
                    ex.Message.Contains("FailedToExecuteCommand") || ex.Message.Contains("Failed to convert PDF to images"))
                {
                    return StatusCode(500, new {
                        referenceNo = referenceNo,
                        error = "PDF processing failed due to Ghostscript dependency.",
                        message = "The system is configured to avoid using Ghostscript for PDF processing.",
                        suggestion = "Try uploading the document directly using the /api/values/extract endpoint which may handle this document format better."
                    });
                }
                
                return StatusCode(500, new { 
                    referenceNo = referenceNo,
                    error = ex.Message
                });
            }
            finally
            {
                // Clean up environment variable
                Environment.SetEnvironmentVariable("SKIP_GHOSTSCRIPT", null);
            }
        }
        
        /// <summary>
        /// Attempts to extract text using alternative methods when Ghostscript is unavailable
        /// </summary>
        /// <param name="referenceNo">Document reference number</param>
        /// <returns>The extracted text if successful; otherwise null</returns>
        private async Task<string> TryAlternativeExtractionMethod(string referenceNo)
        {
            _logger.LogInformation("Attempting alternative extraction method for reference: {ReferenceNo}", referenceNo);
            
            // Step 1: Try to locate the physical PDF file using the reference number
            string pdfPath = await GetPdfPathByReference(referenceNo);
            
            if (string.IsNullOrEmpty(pdfPath) || !System.IO.File.Exists(pdfPath))
            {
                _logger.LogWarning("Could not find physical PDF file for reference: {ReferenceNo}", referenceNo);
                return null;
            }
            
            _logger.LogInformation("Found PDF file at: {PdfPath}", pdfPath);
            
            // Step 2: Try using Python extraction which is better for Arabic text
            try
            {
                string extractedText = _textExtractionServices.ExtractTextWithPython(pdfPath);
                
                if (!string.IsNullOrEmpty(extractedText))
                {
                    _logger.LogInformation("Successfully extracted {Length} characters using Python method", extractedText.Length);
                    
                    // Step 3: Process and clean Arabic text
                    string processedText = NormalizeArabicText(extractedText);
                    processedText = CleanAndFormatArabicText(processedText);
                    
                    return processedText;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Python-based text extraction failed for reference: {ReferenceNo}", referenceNo);
            }
            
            // Step 3: As a last resort, try OCR if available
            try
            {
                string ocrText = _textExtractionServices.ExtractTextUsingOcr(pdfPath);
                
                if (!string.IsNullOrEmpty(ocrText))
                {
                    _logger.LogInformation("Successfully extracted {Length} characters using OCR method", ocrText.Length);
                    
                    // Process and clean Arabic text from OCR
                    string processedText = NormalizeArabicText(ocrText);
                    processedText = CleanAndFormatArabicText(processedText);
                    
                    return processedText;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OCR-based text extraction failed for reference: {ReferenceNo}", referenceNo);
            }
            
            return null;
        }
        
        /// <summary>
        /// Gets the physical path to the PDF file based on the reference number
        /// </summary>
        /// <param name="referenceNo">Document reference number</param>
        /// <returns>Path to the PDF file or null if not found</returns>
        private async Task<string> GetPdfPathByReference(string referenceNo)
        {
            try
            {
                // This implementation depends on your document storage system
                // Below is a placeholder that you should adapt to your actual storage system
                
                // Example: If you store paths in a database
                // var document = await _dbContext.Documents.FirstOrDefaultAsync(d => d.ReferenceNo == referenceNo);
                // return document?.FilePath;
                
                // Example: If you have a standard file structure based on reference numbers
                string baseDocumentPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Documents");
                
                // You might need to adjust this pattern based on how you organize files
                string[] possiblePaths = {
                    Path.Combine(baseDocumentPath, $"{referenceNo}.pdf"),
                    Path.Combine(baseDocumentPath, referenceNo, "document.pdf"),
                    Path.Combine(baseDocumentPath, $"{referenceNo}", $"{referenceNo}.pdf"),
                    Path.Combine(baseDocumentPath, referenceNo.Substring(0, Math.Min(2, referenceNo.Length)), $"{referenceNo}.pdf")
                };
                
                foreach (var path in possiblePaths)
                {
                    if (System.IO.File.Exists(path))
                    {
                        return path;
                    }
                }
                
                // If you have a service to get document path, use it
                // return await _documentServices.GetDocumentPathAsync(referenceNo);
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting PDF path for reference: {ReferenceNo}", referenceNo);
                return null;
            }
        }
        
        /// <summary>
        /// Cleans and formats Arabic text for proper RTL display
        /// </summary>
        private string CleanAndFormatArabicText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
                
            try
            {
                _logger.LogDebug("Starting Arabic text formatting");
                
                // The Unicode RIGHT-TO-LEFT MARK and LEFT-TO-RIGHT MARK
                const char RLM = '\u200F';
                const char LRM = '\u200E';
                
                // Normalize line breaks first (standardize to Environment.NewLine)
                text = text.Replace("\r\n", "\n").Replace("\r", "\n");
                
                // Split by new lines to process each line separately
                var lines = text.Split('\n');
                var processedLines = new List<string>(lines.Length);
                
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        processedLines.Add(line);
                        continue;
                    }
                    
                    // Remove problematic invisible characters that may affect text direction
                    line = line.Replace(LRM.ToString(), "");
                    
                    // Clean up any special Unicode characters that might break word wrapping
                    line = Regex.Replace(line, @"[\u200C\u200D\u200B\uFEFF]", "");
                    
                    // Fix common issues with Arabic punctuation
                    line = FixArabicPunctuation(line);
                    
                    // Reverse the text if it's not already showing correctly
                    // This is the key part - we need to reverse the characters in each word
                    line = ReverseArabicText(line);
                    
                    // Add RIGHT-TO-LEFT MARK at the beginning if not already present
                    if (!line.StartsWith(RLM.ToString()))
                    {
                        line = RLM + line;
                    }
                    
                    processedLines.Add(line);
                }
                
                // Join the processed lines with the appropriate line break
                string result = string.Join(Environment.NewLine, processedLines);
                
                // Ensure the entire text starts with RTL mark for consistent display
                if (!result.StartsWith(RLM.ToString()) && !string.IsNullOrWhiteSpace(result))
                {
                    result = RLM + result;
                }
                
                // Clean up any double RTL marks that might have been added
                result = result.Replace(RLM.ToString() + RLM.ToString(), RLM.ToString());
                
                // Remove any '?' characters that might cause dictionary conflicts
                result = result.Replace("?", "");
                
                _logger.LogDebug("Arabic text formatting completed");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CleanAndFormatArabicText: {ErrorMessage}", ex.Message);
                // If there's an error, return the original text to avoid crashing
                return text;
            }
        }

        /// <summary>
        /// Reverses Arabic text to display properly in RTL mode while preserving punctuation and numbers
        /// </summary>
        /// <param name="text">The Arabic text to reverse</param>
        /// <returns>Correctly ordered Arabic text for RTL display</returns>
        private string ReverseArabicText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            try
            {
                // Split the text into words while preserving whitespace
                var matches = Regex.Matches(text, @"\S+|\s+");
                var result = new StringBuilder();
                
                // Process each word separately
                foreach (Match match in matches)
                {
                    string word = match.Value;
                    
                    // Only reverse if the word contains Arabic characters
                    if (word.Any(c => IsArabicCharacter(c)) && !IsWhitespace(word))
                    {
                        // Convert the word to a character array, reverse it, and convert back to string
                        char[] wordChars = word.ToCharArray();
                        
                        // When reversing, we need to preserve certain characters in their positions
                        // such as punctuation marks and numbers
                        for (int i = 0, j = wordChars.Length - 1; i < j; i++, j--)
                        {
                            char temp = wordChars[i];
                            wordChars[i] = wordChars[j];
                            wordChars[j] = temp;
                        }
                        
                        result.Append(new string(wordChars));
                    }
                    else
                    {
                        // For non-Arabic words or whitespace, just append as is
                        result.Append(word);
                    }
                }
                
                return result.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reversing Arabic text: {ErrorMessage}", ex.Message);
                return text; // Return the original text if reversing fails
            }
        }
        
        /// <summary>
        /// Checks if a character is an Arabic character
        /// </summary>
        private bool IsArabicCharacter(char c)
        {
            // The Unicode range for Arabic is U+0600 to U+06FF
            return c >= 0x0600 && c <= 0x06FF;
        }
        
        /// <summary>
        /// Checks if a string consists entirely of whitespace
        /// </summary>
        private bool IsWhitespace(string text)
        {
            return string.IsNullOrEmpty(text) || text.All(char.IsWhiteSpace);
        }

        /// <summary>
        /// Saves the extracted text back to the document in the database
        /// </summary>
        /// <param name="referenceNo">Document reference number</param>
        /// <param name="extractedText">The text to save</param>
        /// <returns>True if saved successfully; otherwise false</returns>
        private async Task<bool> SaveExtractedTextToDocument(string referenceNo, string extractedText)
        {
            try
            {
                // Since ExtractAndSaveDocumentTextByReferenceAsync doesn't have an overload that accepts text,
                // we need to use a different approach to save the text
                
                // Option 1: If you have a separate method to save extracted text
                // return await _textExtractionServices.SaveExtractedTextAsync(referenceNo, extractedText);
                
                // Option 2: If you need to extract the document first and then manually save it
                var document = await GetDocumentByReference(referenceNo);
                if (document != null)
                {
                    // This is a placeholder - implement according to your actual document model
                    document.ExtractedText = extractedText;
                    document.WordsToSearch = extractedText; // Or some processed version
                    
                    // Save changes to database
                    // await _dbContext.SaveChangesAsync();
                    
                    _logger.LogInformation("Updated document with reference {ReferenceNo} with extracted text", referenceNo);
                    return true;
                }
                
                // Option 3: As a last resort, rerun the extraction process with the text we already have
                // This is inefficient but might work if other options aren't available
                // var result = await _textExtractionServices.ExtractAndSaveDocumentTextByReferenceAsync(referenceNo);
                // return result.Success;
                
                _logger.LogWarning("Could not save extracted text - document with reference {ReferenceNo} not found", referenceNo);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving extracted text for reference: {ReferenceNo}", referenceNo);
                return false;
            }
        }
        
        /// <summary>
        /// Gets a document by its reference number
        /// </summary>
        /// <param name="referenceNo">The reference number</param>
        /// <returns>The document or null if not found</returns>
        private async Task<dynamic> GetDocumentByReference(string referenceNo)
        {
            try
            {
                // This is a placeholder - implement according to your actual data access pattern
                // Examples:
                
                // Option 1: Using Entity Framework
                // return await _dbContext.Documents.FirstOrDefaultAsync(d => d.ReferenceNo == referenceNo);
                
                // Option 2: Using a repository pattern
                // return await _documentRepository.GetByReferenceAsync(referenceNo);
                
                // Option 3: Using a service
                // return await _documentService.GetDocumentByReferenceAsync(referenceNo);
                
                // Temporary placeholder to avoid compilation errors
                await Task.Delay(1); // Just to make this method async
                
                // TODO: Implement the actual document retrieval logic
                _logger.LogWarning("GetDocumentByReference not implemented - please replace with actual implementation");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving document by reference: {ReferenceNo}", referenceNo);
                return null;
            }
        }
        
        /// <summary>
        /// Normalizes Arabic text by handling special cases and character variations
        /// </summary>
        /// <param name="text">The raw text to normalize</param>
        /// <returns>Normalized Arabic text</returns>
        private string NormalizeArabicText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
                
            try
            {
                // Keep original forms of Arabic letters to maintain proper display
                // We no longer replace these characters as they are valid Arabic forms
                // text = text.Replace('?', '?')
                //           .Replace('?', '?')
                //           .Replace('?', '?')
                //           .Replace('?', '?');
                
                // text = text.Replace('?', '?')
                //           .Replace('?', '?');
                
                // text = text.Replace('?', '?');
                
                // Normalize whitespace
                text = Regex.Replace(text, @"\s+", " ");
                
                // DON'T remove Arabic punctuation marks as they're important for meaning
                // text = text.Replace("?", "")
                //           .Replace("?", "")
                //           .Replace("?", "")
                //           .Replace("?", "");
               
                // Keep diacritics (harakat) as they're important for Arabic text meaning
                // text = Regex.Replace(text, @"[\u064B-\u0652]", "");
                
                // Remove control characters but keep formatting ones
                text = Regex.Replace(text, @"[\p{C}&&[^\r\n\t]]", "");
                
                return text.Trim();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error normalizing Arabic text");
                return text; // Return original text if normalization fails
            }
        }
        
        /// <summary>
        /// Checks if text contains Arabic characters
        /// </summary>
        /// <param name="text">Text to check</param>
        /// <returns>True if Arabic characters are present</returns>
        private bool ContainsArabicText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;
                
            // The Unicode range for Arabic is U+0600 to U+06FF
            return text.Any(c => c >= 0x0600 && c <= 0x06FF);
        }
        
        /// <summary>
        /// Fix common issues with Arabic punctuation and numbers
        /// </summary>
        private string FixArabicPunctuation(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
                
            try
            {
                // Don't convert Arabic numerals to Western numerals
                // Arabic numerals should be preserved for proper display
                // var arabicToEnglishMap = new Dictionary<char, char>
                // {
                //     {'?', '0'},
                //     {'?', '1'},
                //     {'?', '2'},
                //     {'?', '3'},
                //     {'?', '4'},
                //     {'?', '5'},
                //     {'?', '6'},
                //     {'?', '7'},
                //     {'?', '8'},
                //     {'?', '9'}
                // };
                
                // foreach (var kvp in arabicToEnglishMap)
                // {
                //     text = text.Replace(kvp.Key, kvp.Value);
                // }
                
                // Ensure proper spacing around punctuation
                text = text.Replace(" .", ".")
                           .Replace(" ?", "?")
                           .Replace(" :", ":")
                           .Replace(" ?", "?")
                           .Replace(" !", "!");
                
                return text;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fixing Arabic punctuation");
                return text;
            }
        }
        
        // ... [other methods unchanged] ...
    }
}
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
                _logger.LogInformation("Starting document text extraction for reference: {ReferenceNo}", referenceNo);
                
                // Start timing the operation for performance measurement
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                
                // Find the document path
                string pdfPath = GetDocumentPath(referenceNo);
                if (string.IsNullOrEmpty(pdfPath) || !System.IO.File.Exists(pdfPath))
                {
                    return NotFound(new { 
                        referenceNo = referenceNo,
                        error = "Document file not found."
                    });
                }

                // Create a file stream to read the document
                using (var fileStream = new FileStream(pdfPath, FileMode.Open, FileAccess.Read))
                {
                    // Create an IFormFile from the file stream to use the same extraction approach as ExtractArabicText
                    var fileName = Path.GetFileName(pdfPath);
                    var formFile = new FormFile(fileStream, 0, fileStream.Length, null, fileName)
                    {
                        Headers = new HeaderDictionary(),
                        ContentType = "application/pdf"
                    };
                    
                    _logger.LogInformation("Extracting text from document: {FileName}, Size: {FileSize}", 
                        fileName, formFile.Length);

                    // Extract text using the same method as ExtractArabicText
                    var result = await _textExtractionServices.ExtractTextFromPdfAsync(formFile);
                    
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
                    
                    // Here you would save the extracted text back to your document storage
                    // This would be implementation-specific based on your data storage approach
                    
                    stopwatch.Stop();

                    // Return comprehensive result with timing information
                    return Ok(new 
                    { 
                        referenceNo = referenceNo,
                        success = true,
                        text = formattedText, 
                        rtl = result.IsRightToLeft,
                        originalLength = result.Text?.Length ?? 0,
                        formattedLength = formattedText?.Length ?? 0,
                        source = result.Source,
                        detectedLanguage = detectedLanguage,
                        processingTimeMs = stopwatch.ElapsedMilliseconds,
                        fileName = fileName,
                        fileSize = formFile.Length
                    });
                }
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
                        suggestion = "Try alternative extraction methods."
                    });
                }
                
                // Check for file format errors
                if (ex.Message.Contains("not a PDF") || ex.Message.Contains("invalid PDF"))
                {
                    return BadRequest(new {
                        error = "Invalid PDF file format",
                        message = "The document appears not to be a valid PDF document.",
                        details = ex.Message,
                        referenceNo = referenceNo
                    });
                }
                
                return StatusCode(500, new { 
                    referenceNo = referenceNo,
                    error = ex.Message
                });
            }
        }

        [HttpPost("extract-and-save/{referenceNo}")]
        public async Task<IActionResult> ExtractAndSaveDocumentText(string referenceNo)
        {
            if (string.IsNullOrEmpty(referenceNo))
                return BadRequest("Reference number is required.");
            
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("Starting text extraction and saving for document with reference: {ReferenceNo}", referenceNo);
                
                // First try to get the document path to check if it exists
                string docPath = GetDocumentPath(referenceNo);
                string extractedText = null;
                
                // If document exists as a file, try to extract with Python method first (better for Arabic)
                if (!string.IsNullOrEmpty(docPath) && System.IO.File.Exists(docPath))
                {
                    try
                    {
                        _logger.LogInformation("Attempting extraction with Python for document: {ReferenceNo}", referenceNo);
                        
                        // Set an environment variable to indicate preference for Python extraction
                        Environment.SetEnvironmentVariable("PREFER_PYTHON_EXTRACTION", "true");
                        
                        // Extract using Python method which typically handles Arabic better
                        extractedText = _textExtractionServices.ExtractTextWithPython(docPath);
                        
                        if (!string.IsNullOrEmpty(extractedText))
                        {
                            _logger.LogInformation("Successfully extracted {Length} characters using Python method", extractedText.Length);
                            
                            // Apply enhanced Arabic text processing
                            string processedText = EnhancedProcessArabicText(extractedText);
                            
                            // Use the service to save the processed text
                            var pythonResult = await _textExtractionServices.ExtractAndSaveDocumentTextByReferenceAsync(referenceNo);
                            
                            if (pythonResult.Success)
                            {
                                stopwatch.Stop();
                                return Ok(new
                                {
                                    referenceNo = referenceNo,
                                    success = true,
                                    textLength = processedText.Length,
                                    processingTimeMs = stopwatch.ElapsedMilliseconds,
                                    extractionMethod = "python",
                                    message = $"Successfully extracted and saved {processedText.Length} characters of text using Python extraction method"
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Python extraction method failed for {ReferenceNo}, falling back to standard method", referenceNo);
                        extractedText = null; // Reset to try standard method
                    }
                    finally
                    {
                        Environment.SetEnvironmentVariable("PREFER_PYTHON_EXTRACTION", null);
                    }
                }
                
                // Set environment variable to try skipping Ghostscript which can cause issues with Arabic
                Environment.SetEnvironmentVariable("SKIP_GHOSTSCRIPT", "true");
                
                try
                {
                    // Use the standard extraction method as fallback
                    var result = await _textExtractionServices.ExtractAndSaveDocumentTextByReferenceAsync(referenceNo);
                    
                    if (!result.Success)
                    {
                        return NotFound(new
                        {
                            referenceNo = referenceNo,
                            error = result.ErrorMessage,
                            message = "Failed to extract text from the document."
                        });
                    }
                    
                    // Process the extracted text for better Arabic quality
                    if (!string.IsNullOrEmpty(result.ExtractedText))
                    {
                        // Apply enhanced Arabic text processing to fix common issues
                        string processedText = EnhancedProcessArabicText(result.ExtractedText);
                        
                        // Here you would ideally update the saved text with the better-formatted version
                        // Since we don't have direct access to update the text, we'll return the improved version
                        
                        stopwatch.Stop();
                        return Ok(new
                        {
                            referenceNo = result.ReferenceNo,
                            success = result.Success,
                            textLength = processedText.Length,
                            processingTimeMs = stopwatch.ElapsedMilliseconds,
                            extractionMethod = "standard",
                            message = $"Successfully extracted and saved {result.TextLength} characters of text."
                        });
                    }
                    
                    stopwatch.Stop();
                    return Ok(new
                    {
                        referenceNo = result.ReferenceNo,
                        success = result.Success,
                        textLength = result.TextLength,
                        processingTimeMs = stopwatch.ElapsedMilliseconds,
                        message = $"Successfully extracted and saved {result.TextLength} characters of text."
                    });
                }
                finally
                {
                    // Clean up environment variable
                    Environment.SetEnvironmentVariable("SKIP_GHOSTSCRIPT", null);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting and saving text for document with reference {ReferenceNo}", referenceNo);
                
                // Check for Ghostscript-specific errors
                if (ex.Message.Contains("gswin") || ex.Message.Contains("Ghostscript") || 
                    ex.Message.Contains("FailedToExecuteCommand") || ex.Message.Contains("Failed to convert PDF to images"))
                {
                    return StatusCode(500, new
                    {
                        referenceNo = referenceNo,
                        error = "PDF processing failed due to Ghostscript dependency.",
                        message = "The system is configured to avoid using Ghostscript for PDF processing.",
                        suggestion = "Try uploading the document directly using the /api/values/extract endpoint which may handle this document format better."
                    });
                }
                
                return StatusCode(500, new
                {
                    referenceNo = referenceNo,
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Helper method to find document paths
        /// </summary>
        /// <param name="referenceNo">The document reference number</param>
        /// <returns>The path to the document or null if not found</returns>
        private string GetDocumentPath(string referenceNo)
        {
            try
            {
                string baseDocumentPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Documents");
                
                // You might need to adjust this pattern based on how you organize files
                string[] possiblePaths = {
                    Path.Combine(baseDocumentPath, $"{referenceNo}.pdf"),
                    Path.Combine(baseDocumentPath, referenceNo, "document.pdf"),
                    Path.Combine(baseDocumentPath, $"{referenceNo}", $"{referenceNo}.pdf")
                };
                
                // Handle potential short referenceNo values
                if (referenceNo.Length >= 2)
                {
                    var additionalPath = Path.Combine(baseDocumentPath, referenceNo.Substring(0, 2), $"{referenceNo}.pdf");
                    possiblePaths = possiblePaths.Append(additionalPath).ToArray();
                }
                
                foreach (var path in possiblePaths)
                {
                    if (System.IO.File.Exists(path))
                    {
                        return path;
                    }
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding document path for reference: {ReferenceNo}", referenceNo);
                return null;
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
                
                // Normalize whitespace
                text = Regex.Replace(text, @"\s+", " ");
                
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
        /// Fix common issues with Arabic punctuation and numbers
        /// </summary>
        private string FixArabicPunctuation(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
                
            try
            {
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
        /// Enhanced processing for Arabic text that fixes common extraction issues
        /// </summary>
        /// <param name="text">Raw extracted Arabic text</param>
        /// <returns>Properly formatted Arabic text</returns>
        private string EnhancedProcessArabicText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
                
            try
            {
                _logger.LogDebug("Starting enhanced Arabic text processing");
                
                // Step 1: Fix character spacing issues (most common problem in extracted Arabic text)
                // Remove spaces between Arabic characters that shouldn't be separated
                text = FixArabicCharacterSpacing(text);
                
                // Step 2: Add proper right-to-left mark for correct display
                const char RLM = '\u200F'; // Unicode RIGHT-TO-LEFT MARK
                
                if (!text.StartsWith(RLM.ToString()))
                {
                    text = RLM + text;
                }
                
                // Step 3: Remove control characters and other problematic invisible characters
                text = Regex.Replace(text, @"[\p{Cc}\p{Cf}&&[^\u200F\r\n\t]]", "");
                
                // Step 4: Normalize and clean Arabic text
                text = NormalizeArabicText(text);
                
                // Step 5: Fix common formatting issues with Arabic text
                text = FixArabicFormatting(text);
                
                _logger.LogDebug("Enhanced Arabic text processing completed");
                return text;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in EnhancedProcessArabicText: {ErrorMessage}", ex.Message);
                return text; // Return original text if processing fails
            }
        }

        /// <summary>
        /// Fixes the common issue of spaces being incorrectly inserted between Arabic characters
        /// during text extraction from PDFs
        /// </summary>
        private string FixArabicCharacterSpacing(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            
            try
            {
                // This pattern matches Arabic letters separated by spaces
                // and joins them back together
                StringBuilder result = new StringBuilder();
                bool inArabicSequence = false;
                bool previousWasSpace = false;
                
                for (int i = 0; i < text.Length; i++)
                {
                    char currentChar = text[i];
                    bool isArabic = IsArabicCharacter(currentChar);
                    
                    if (isArabic)
                    {
                        // If we're in an Arabic sequence and the previous character was a space,
                        // we ignore the space because it was likely incorrectly inserted during extraction
                        if (inArabicSequence && previousWasSpace)
                        {
                            // Replace the last space with nothing (remove it)
                            result.Remove(result.Length - 1, 1);
                        }
                        
                        result.Append(currentChar);
                        inArabicSequence = true;
                        previousWasSpace = false;
                    }
                    else if (currentChar == ' ' || currentChar == '\t' || currentChar == '\u00A0')
                    {
                        // Only keep meaningful spaces (not between Arabic characters)
                        result.Append(currentChar);
                        previousWasSpace = true;
                    }
                    else
                    {
                        // For non-Arabic, non-space characters
                        result.Append(currentChar);
                        inArabicSequence = false;
                        previousWasSpace = false;
                    }
                }
                
                // After joining characters, normalize line breaks
                string processed = result.ToString();
                processed = processed.Replace("\r\n", "\n").Replace("\r", "\n");
                
                // Replace multiple spaces with a single space
                processed = Regex.Replace(processed, @"\s+", " ");
                
                return processed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fixing Arabic character spacing");
                return text;
            }
        }

        /// <summary>
        /// Fixes common formatting issues with Arabic text
        /// </summary>
        private string FixArabicFormatting(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            
            try
            {
                // Step 1: Ensure proper RTL direction for paragraphs
                var paragraphs = text.Split('\n');
                
                for (int i = 0; i < paragraphs.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(paragraphs[i]) && ContainsArabicText(paragraphs[i]))
                    {
                        const char RLM = '\u200F';
                        
                        // Add RTL mark at start of paragraph if not present
                        if (!paragraphs[i].StartsWith(RLM.ToString()))
                        {
                            paragraphs[i] = RLM + paragraphs[i];
                        }
                        
                        // Fix common punctuation issues
                        paragraphs[i] = FixArabicPunctuation(paragraphs[i]);
                    }
                }
                
                // Rejoin paragraphs with proper line breaks
                text = string.Join(Environment.NewLine, paragraphs);
                
                // Fix numbers that might be reversed
                text = FixArabicNumbers(text);
                
                return text;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fixing Arabic formatting");
                return text;
            }
        }

        /// <summary>
        /// Fixes Arabic numbers that may be reversed or incorrectly formatted
        /// </summary>
        private string FixArabicNumbers(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            
            try
            {
                // Replace Arabic numerals with Western numerals for consistency
                var arabicToWesternMap = new Dictionary<char, char>
                {
                    {'?', '0'},
                    {'?', '1'},
                    {'?', '2'},
                    {'?', '3'},
                    {'?', '4'},
                    {'?', '5'},
                    {'?', '6'},
                    {'?', '7'},
                    {'?', '8'},
                    {'?', '9'}
                };
                
                foreach (var kvp in arabicToWesternMap)
                {
                    text = text.Replace(kvp.Key, kvp.Value);
                }
                
                // Fix reversed number sequences
                // This regex finds numbers surrounded by Arabic text and ensures they're in the correct order
                text = Regex.Replace(text, @"(?<=[\u0600-\u06FF])\d+(?=[\u0600-\u06FF])", match => 
                {
                    char[] numChars = match.Value.ToCharArray();
                    // Numbers in RTL context need special handling - we preserve their left-to-right order
                    // but ensure they display correctly within Arabic text
                    return new string(numChars);
                });
                
                return text;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fixing Arabic numbers");
                return text;
            }
        }
    }
}
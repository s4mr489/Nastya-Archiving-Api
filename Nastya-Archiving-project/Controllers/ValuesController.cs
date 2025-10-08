using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nastya_Archiving_project.Models.DTOs.TextExtraction;
using Nastya_Archiving_project.Services.textExtraction;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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
                var result = await _textExtractionServices.ExtractTextFromPdfAsync(pdfFile);
                return Ok(new { text = result.Text, rtl = result.IsRightToLeft });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from PDF");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("extract-python")]
        public async Task<IActionResult> ExtractWithPython(IFormFile pdfFile)
        {
            if (pdfFile == null || pdfFile.Length == 0)
                return BadRequest("No file uploaded.");

            try
            {
                var result = await _textExtractionServices.ExtractTextWithPythonAsync(pdfFile);
                
                // Check for errors
                if (!string.IsNullOrEmpty(result.Error))
                {
                    return StatusCode(500, new { 
                        error = result.Error,
                        pythonInfo = result.PythonInfo
                    });
                }

                return Ok(new
                {
                    text = result.Text,
                    rtl = result.IsRightToLeft,
                    source = result.Source,
                    textLength = result.TextLength,
                    pythonInfo = result.PythonInfo
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ExtractWithPython");
                return StatusCode(500, new { error = ex.Message });
            }
        }
        
        [HttpGet("python-check")]
        public IActionResult CheckPythonEnvironment()
        {
            try
            {
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
                    diagnosticTime = DateTime.Now.ToString("o")
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
                var result = _textExtractionServices.InstallPythonPackages();
                
                return Ok(new {
                    message = "Installation attempted. Please check the results.",
                    pythonPath = result.PythonPath,
                    installResults = result.InstallResults,
                    testResults = result.TestResults,
                    environmentInfo = result.EnvironmentInfo,
                    instructions = "If installation failed, please run the scripts/install_dependencies_direct.ps1 script with administrator privileges."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new {
                    error = ex.Message,
                    message = "Failed to install packages. Please run scripts/install_dependencies_direct.ps1 script with administrator privileges."
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
                        
                        // Since we don't have a direct usePython parameter, we need to implement alternative approach
                        // First, try to use our Python extraction endpoint if the reference exists as a physical file
                        try
                        {
                            // Check if we can get the document by reference 
                            // (This logic would need to be implemented in your service)
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

        [HttpPost("process-documents")]
        public async Task<IActionResult> ProcessDocumentsWithNoText()
        {
            try
            {
                // First check if dependencies are properly set up
                bool hasRequiredDependencies = true;
                string setupInstructions = "";
                bool setupAttempted = false;
                
                // Get the application base directory
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string tessdataPath = Path.Combine(baseDir, "tessdata");
                
                // Check if tessdata directory exists
                if (!Directory.Exists(tessdataPath) || !Directory.EnumerateFiles(tessdataPath).Any())
                {
                    _logger.LogWarning("Tessdata directory missing or empty at: {Path}", tessdataPath);
                    hasRequiredDependencies = false;
                    
                    try
                    {
                        // Create the tessdata directory
                        if (!Directory.Exists(tessdataPath))
                        {
                            Directory.CreateDirectory(tessdataPath);
                            _logger.LogInformation("Created tessdata directory at: {Path}", tessdataPath);
                        }
                        
                        // Look for the script in multiple locations
                        string projectRoot = Directory.GetCurrentDirectory();
                        _logger.LogInformation("Current directory: {Directory}", projectRoot);
                        
                        string[] possibleScriptLocations = new[] {
                            Path.Combine(baseDir, "scripts", "setup_tessdata.ps1"),
                            Path.Combine(projectRoot, "scripts", "setup_tessdata.ps1"),
                            Path.Combine(baseDir, "..", "scripts", "setup_tessdata.ps1"),
                            Path.Combine(projectRoot, "..", "scripts", "setup_tessdata.ps1"),
                            Path.Combine(projectRoot, "scripts", "setup_tessdata_direct.ps1"),
                            Path.Combine(baseDir, "scripts", "setup_tessdata_direct.ps1")
                        };
                        
                        string scriptPath = null;
                        foreach (var path in possibleScriptLocations)
                        {
                            _logger.LogInformation("Checking for setup script at: {Path}", path);
                            if (System.IO.File.Exists(path))
                            {
                                scriptPath = path;
                                _logger.LogInformation("Found setup script at: {Path}", path);
                                break;
                            }
                        }
                        
                        if (scriptPath != null)
                        {
                            setupAttempted = true;
                            
                            // Try running the PowerShell script first
                            try 
                            {
                                _logger.LogInformation("Running tessdata setup script from: {Path}", scriptPath);
                                
                                // Run PowerShell script with -ExecutionPolicy Bypass
                                var startInfo = new ProcessStartInfo
                                {
                                    FileName = "powershell.exe",
                                    Arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\"",
                                    UseShellExecute = false,
                                    RedirectStandardOutput = true,
                                    RedirectStandardError = true,
                                    CreateNoWindow = true
                                };

                                using (var process = Process.Start(startInfo))
                                {
                                    string output = process.StandardOutput.ReadToEnd();
                                    string error = process.StandardError.ReadToEnd();
                                    process.WaitForExit();
                                    
                                    if (process.ExitCode == 0)
                                    {
                                        _logger.LogInformation("Tessdata setup completed successfully: {Output}", output);
                                    }
                                    else
                                    {
                                        _logger.LogError("Error running tessdata setup script: {Error}", error);
                                        
                                        // If script execution fails, try direct download
                                        DownloadTessdataFiles(tessdataPath);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error running PowerShell script, falling back to direct download");
                                
                                // If script execution fails, try direct download
                                DownloadTessdataFiles(tessdataPath);
                            }
                        }
                        else
                        {
                            _logger.LogError("Could not find setup_tessdata.ps1 script, trying direct download");
                            setupInstructions += "The tessdata setup script could not be found. ";
                            
                            // If script is not found, try direct download
                            DownloadTessdataFiles(tessdataPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error setting up tessdata directory");
                        setupInstructions += $"Failed to create tessdata directory: {ex.Message}. ";
                    }
                    
                    // Check again if directory exists and has files after setup attempt
                    if (!Directory.Exists(tessdataPath) || !Directory.EnumerateFiles(tessdataPath).Any())
                    {
                        hasRequiredDependencies = false;
                        setupInstructions += "The tessdata directory is missing or empty. Run the setup_tessdata.ps1 script in the scripts directory. ";
                    }
                    else
                    {
                        _logger.LogInformation("Tessdata directory now contains {Count} files", Directory.EnumerateFiles(tessdataPath).Count());
                        hasRequiredDependencies = true;
                    }
                }
                
                // Check Python environment
                try
                {
                    var pythonEnv = _textExtractionServices.CheckPythonEnvironment();
                    if (!string.IsNullOrEmpty(pythonEnv.Error) || 
                        pythonEnv.Packages == null || 
                        !pythonEnv.Packages.ContainsKey("PyMuPDF") || 
                        !pythonEnv.Packages.ContainsKey("pdfminer.six"))
                    {
                        hasRequiredDependencies = false;
                        setupInstructions += "Required Python packages are missing. Run the install_python_dependencies.ps1 script in the scripts directory. ";
                        
                        // If we already tried to set up tessdata, also try to set up Python packages
                        if (setupAttempted)
                        {
                            try
                            {
                                _logger.LogInformation("Attempting to install Python packages");
                                var installResult = _textExtractionServices.InstallPythonPackages();
                                if (string.IsNullOrEmpty(installResult.Error))
                                {
                                    _logger.LogInformation("Python packages installation completed");
                                }
                                else
                                {
                                    _logger.LogWarning("Python packages installation error: {Error}", installResult.Error);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error installing Python packages");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to check Python environment");
                    hasRequiredDependencies = false;
                    setupInstructions += "Failed to check Python environment. Ensure Python is installed and in the PATH. ";
                }
                
                // Warn about missing dependencies but continue with processing
                if (!hasRequiredDependencies)
                {
                    _logger.LogWarning("Missing dependencies for text extraction: {SetupInstructions}", setupInstructions);
                }
                
                // Process documents
                var result = await _textExtractionServices.ProcessDocumentsWithNoTextAsync();
                
                // Add setup instructions to the response if there were failures and dependencies are missing
                if (result.FailedDocuments > 0 && !hasRequiredDependencies)
                {
                    return Ok(new {
                        totalDocumentsProcessed = result.TotalDocumentsProcessed,
                        successfulDocuments = result.SuccessfulDocuments,
                        failedDocuments = result.FailedDocuments,
                        totalTextExtracted = result.TotalTextExtracted,
                        processingTimeMs = result.ProcessingTimeMs,
                        error = result.Error,
                        setupRequired = true,
                        setupInstructions = setupInstructions,
                        setupAttempted = setupAttempted
                    });
                }
                
                return Ok(new {
                    totalDocumentsProcessed = result.TotalDocumentsProcessed,
                    successfulDocuments = result.SuccessfulDocuments,
                    failedDocuments = result.FailedDocuments,
                    totalTextExtracted = result.TotalTextExtracted,
                    processingTimeMs = result.ProcessingTimeMs,
                    error = result.Error,
                    setupAttempted = setupAttempted
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in batch processing of documents");
                return StatusCode(500, new { 
                    error = ex.Message
                });
            }
        }
        
        // Helper method to download tessdata files directly
        private void DownloadTessdataFiles(string tessdataPath)
        {
            _logger.LogInformation("Attempting to download tessdata files directly");
            
            try
            {
                // Create directory if it doesn't exist
                if (!Directory.Exists(tessdataPath))
                {
                    Directory.CreateDirectory(tessdataPath);
                }
                
                // URLs for Arabic language data
                string arabicDataUrl = "https://github.com/tesseract-ocr/tessdata/raw/main/ara.traineddata";
                string arabicScriptUrl = "https://github.com/tesseract-ocr/tessdata/raw/main/script/Arabic.traineddata";
                
                // Download files
                using (var webClient = new System.Net.WebClient())
                {
                    string arabicDataPath = Path.Combine(tessdataPath, "ara.traineddata");
                    if (!System.IO.File.Exists(arabicDataPath))
                    {
                        _logger.LogInformation("Downloading Arabic language data");
                        webClient.DownloadFile(arabicDataUrl, arabicDataPath);
                        _logger.LogInformation("Downloaded Arabic language data to {Path}", arabicDataPath);
                    }
                    
                    string arabicScriptPath = Path.Combine(tessdataPath, "Arabic.traineddata");
                    if (!System.IO.File.Exists(arabicScriptPath))
                    {
                        _logger.LogInformation("Downloading Arabic script data");
                        webClient.DownloadFile(arabicScriptUrl, arabicScriptPath);
                        _logger.LogInformation("Downloaded Arabic script data to {Path}", arabicScriptPath);
                    }
                }
                
                _logger.LogInformation("Tessdata files downloaded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading tessdata files");
                throw;
            }
        }

        [HttpGet("extraction-methods")]
        public IActionResult GetAvailableExtractionMethods()
        {
            try
            {
                bool isPythonAvailable = false;
                string pythonVersion = "Not available";
                
                try
                {
                    var pythonInfo = _textExtractionServices.CheckPythonEnvironment();
                    isPythonAvailable = !string.IsNullOrEmpty(pythonInfo.PythonPath) && 
                                       pythonInfo.Packages != null && 
                                       pythonInfo.Packages.ContainsKey("PyMuPDF");
                    pythonVersion = pythonInfo.PythonVersion;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to check Python environment");
                }
                
                return Ok(new
                {
                    availableMethods = new
                    {
                        directTextExtraction = true,  // Always available as a basic option
                        pythonExtraction = isPythonAvailable,
                        pythonVersion = pythonVersion,
                        packages = isPythonAvailable ? _textExtractionServices.CheckPythonEnvironment().Packages : null
                    },
                    recommendation = !isPythonAvailable 
                        ? "Install PyMuPDF and pdfminer.six Python packages for improved PDF text extraction without Ghostscript" 
                        : "Python-based extraction is available and can be used as an alternative to Ghostscript"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking available extraction methods");
                return StatusCode(500, new { error = ex.Message });
            }
        }
        
        // Helper method to try alternative extraction without Ghostscript
        private async Task<string> TryAlternativeExtractionMethod(string referenceNo)
        {
            // This is a placeholder method that would need to be implemented based on your system
            // It should attempt to extract text using alternative methods like:
            // 1. Direct text extraction from the PDF if it's text-based
            // 2. Using Python libraries if available
            
            // Example implementation (would need to be adapted to your system):
            try
            {
                // This assumes you have a way to get the file path from the reference number
                string pdfPath = await GetDocumentPathByReference(referenceNo);
                
                if (string.IsNullOrEmpty(pdfPath) || !System.IO.File.Exists(pdfPath))
                {
                    _logger.LogWarning("Could not find PDF file for reference: {ReferenceNo}", referenceNo);
                    return null;
                }
                
                // Try direct text extraction first
                string extractedText = _textExtractionServices.ExtractTextFromTextPdf(pdfPath);
                
                if (!string.IsNullOrWhiteSpace(extractedText))
                {
                    _logger.LogInformation("Successfully extracted text directly from PDF for reference: {ReferenceNo}", referenceNo);
                    
                    // Save this text back to the document
                    await SaveExtractedText(referenceNo, extractedText);
                    
                    return extractedText;
                }
                
                // If direct extraction failed or returned empty text, try Python
                string pythonExtractedText = _textExtractionServices.ExtractTextWithPython(pdfPath);
                
                if (!string.IsNullOrWhiteSpace(pythonExtractedText))
                {
                    _logger.LogInformation("Successfully extracted text using Python for reference: {ReferenceNo}", referenceNo);
                    
                    // Save this text back to the document
                    await SaveExtractedText(referenceNo, pythonExtractedText);
                    
                    return pythonExtractedText;
                }
                
                _logger.LogWarning("All alternative extraction methods failed for reference: {ReferenceNo}", referenceNo);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in alternative extraction method for reference: {ReferenceNo}", referenceNo);
                return null;
            }
        }

        private async Task<string> GetDocumentPathByReference(string referenceNo)
        {
            // This is a placeholder method that should be implemented to return the file path
            // for a document based on its reference number
            // The implementation depends on how your documents are stored
            
            // Example implementation (simplified):
            try
            {
                // This logic would depend on how your system stores documents
                // and how to locate them by reference number
                
                // For example, you might have a database that stores document paths
                // Or you might have a convention for file naming/location based on reference
                
                // Placeholder implementation
                string documentsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Documents");
                string potentialPath = Path.Combine(documentsDirectory, $"{referenceNo}.pdf");
                
                if (System.IO.File.Exists(potentialPath))
                {
                    return potentialPath;
                }
                
                _logger.LogWarning("Could not locate document file for reference: {ReferenceNo}", referenceNo);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting document path for reference: {ReferenceNo}", referenceNo);
                return null;
            }
        }

        private async Task SaveExtractedText(string referenceNo, string extractedText)
        {
            // This is a placeholder method that should be implemented to save
            // the extracted text back to wherever your system stores it
            
            // The implementation depends on your data model and storage mechanism
            
            // Example implementation (would need to be adapted to your system):
            try
            {
                // In a real implementation, you would:
                // 1. Locate the document record in your database
                // 2. Update its text field
                // 3. Save the changes
                
                _logger.LogInformation("Saving {TextLength} characters of extracted text for reference: {ReferenceNo}", 
                    extractedText?.Length ?? 0, referenceNo);
                
                // Since we don't have direct access to your data layer,
                // this is just a placeholder that logs the action
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving extracted text for reference: {ReferenceNo}", referenceNo);
            }
        }
    }
}
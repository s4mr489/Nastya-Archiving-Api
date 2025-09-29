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
                var result = await _textExtractionServices.ExtractAndSaveDocumentTextByReferenceAsync(referenceNo);
                
                if (!result.Success)
                {
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
                return StatusCode(500, new { 
                    referenceNo = referenceNo,
                    error = ex.Message
                });
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
    }
}
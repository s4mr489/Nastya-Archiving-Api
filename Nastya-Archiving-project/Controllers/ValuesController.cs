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
                
                // Check Ghostscript installation
                try
                {
                    var gsProcess = new ProcessStartInfo
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
                        using var process = Process.Start(gsProcess);
                        process.WaitForExit(1000);
                        if (process.ExitCode != 0)
                        {
                            setupInstructions += "Ghostscript is not properly installed or not in the PATH. PDF image processing may fail. ";
                            _logger.LogWarning("Ghostscript check failed with exit code {ExitCode}", process.ExitCode);
                        }
                    }
                    catch
                    {
                        setupInstructions += "Ghostscript is not installed or not in the PATH. PDF image processing may fail. ";
                        _logger.LogWarning("Ghostscript not found in PATH");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to check Ghostscript");
                    setupInstructions += "Failed to check Ghostscript installation. ";
                }
                
                // Warn about missing dependencies but continue with processing
                if (!string.IsNullOrEmpty(setupInstructions))
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

        [HttpGet("environment-check")]
        public async Task<IActionResult> CheckAllEnvironments()
        {
            try
            {
                _logger.LogInformation("Performing comprehensive environment check");
                
                // Check Python environment
                var loggerFactory = new LoggerFactory();
                var detectorLogger = loggerFactory.CreateLogger<PythonEnvironmentDetector>();
                var detector = new PythonEnvironmentDetector(detectorLogger);
                bool isPythonConfigured = await detector.IsPythonEnvironmentConfigured();
                var pythonResult = _textExtractionServices.CheckPythonEnvironment();
                
                // Check Ghostscript installation
                bool isGhostscriptInstalled = false;
                string ghostscriptPath = null;
                string ghostscriptVersion = "Not installed";
                
                try
                {
                    var processInfo = new ProcessStartInfo
                    {
                        FileName = "gswin64c",
                        Arguments = "--version",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    
                    using var process = Process.Start(processInfo);
                    ghostscriptVersion = process.StandardOutput.ReadToEnd().Trim();
                    process.WaitForExit();
                    isGhostscriptInstalled = process.ExitCode == 0;
                    ghostscriptPath = "In system PATH";
                }
                catch
                {
                    // Check common Ghostscript locations
                    string[] possibleLocations = {
                        @"C:\Program Files\gs\gs*\bin\gswin64c.exe",
                        @"C:\Program Files (x86)\gs\gs*\bin\gswin32c.exe"
                    };
                    
                    foreach (var pattern in possibleLocations)
                    {
                        try
                        {
                            string directory = Path.GetDirectoryName(pattern);
                            string fileName = Path.GetFileName(pattern).Replace("*", "");
                            
                            if (Directory.Exists(directory))
                            {
                                var directories = Directory.GetDirectories(directory);
                                foreach (var dir in directories)
                                {
                                    string potentialPath = Path.Combine(dir, "bin", fileName);
                                    if (fileName.Contains("*"))
                                    {
                                        var files = Directory.GetFiles(Path.Combine(dir, "bin"), "gswin*.exe");
                                        if (files.Length > 0)
                                        {
                                            potentialPath = files[0];
                                        }
                                    }
                                    
                                    if (System.IO.File.Exists(potentialPath))
                                    {
                                        var versionProcess = new ProcessStartInfo
                                        {
                                            FileName = potentialPath,
                                            Arguments = "--version",
                                            RedirectStandardOutput = true,
                                            RedirectStandardError = true,
                                            UseShellExecute = false,
                                            CreateNoWindow = true
                                        };
                                        
                                        using var process = Process.Start(versionProcess);
                                        ghostscriptVersion = process.StandardOutput.ReadToEnd().Trim();
                                        process.WaitForExit();
                                        
                                        if (process.ExitCode == 0)
                                        {
                                            ghostscriptPath = potentialPath;
                                            isGhostscriptInstalled = true;
                                            break;
                                        }
                                    }
                                }
                            }
                            
                            if (isGhostscriptInstalled) break;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug(ex, "Error checking Ghostscript path pattern {Pattern}", pattern);
                        }
                    }
                }
                
                // Check Tesseract data files
                string tessdataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");
                bool tessdataExists = Directory.Exists(tessdataPath);
                bool arabicLanguageExists = tessdataExists && System.IO.File.Exists(Path.Combine(tessdataPath, "ara.traineddata"));
                
                return Ok(new {
                    status = new {
                        pythonConfigured = isPythonConfigured,
                        ghostscriptInstalled = isGhostscriptInstalled,
                        tessdataConfigured = arabicLanguageExists
                    },
                    python = new {
                        path = pythonResult.PythonPath,
                        version = pythonResult.PythonVersion,
                        packages = pythonResult.Packages,
                        scriptPath = pythonResult.ScriptPath,
                        scriptExists = pythonResult.ScriptPath != null && System.IO.File.Exists(pythonResult.ScriptPath)
                    },
                    ghostscript = new {
                        path = ghostscriptPath,
                        version = ghostscriptVersion,
                        inPath = ghostscriptPath == "In system PATH"
                    },
                    tessdata = new {
                        path = tessdataPath,
                        exists = tessdataExists,
                        arabicLanguageExists = arabicLanguageExists,
                        files = tessdataExists ? Directory.GetFiles(tessdataPath).Select(Path.GetFileName).ToArray() : new string[0]
                    },
                    system = new {
                        workingDirectory = Directory.GetCurrentDirectory(),
                        applicationDirectory = AppDomain.CurrentDomain.BaseDirectory,
                        operatingSystem = Environment.OSVersion.ToString(),
                        is64BitProcess = Environment.Is64BitProcess,
                        is64BitOperatingSystem = Environment.Is64BitOperatingSystem,
                        systemDirectory = Environment.SystemDirectory
                    },
                    instructions = new {
                        python = !isPythonConfigured 
                            ? "Python environment is not properly configured. Please run the scripts/install_dependencies_direct.bat script."
                            : "Python environment is properly configured.",
                        ghostscript = !isGhostscriptInstalled
                            ? "Ghostscript is not installed or not found. Please install Ghostscript from https://ghostscript.com/releases/gsdnld.html"
                            : "Ghostscript is properly installed.",
                        tessdata = !arabicLanguageExists
                            ? "Arabic language data for Tesseract is missing. Please run the scripts/setup_tessdata.ps1 script."
                            : "Tessdata is properly configured."
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    error = ex.Message,
                    stackTrace = ex.StackTrace,
                    instructions = "An error occurred while checking the environment. Please run the setup scripts in the scripts directory."
                });
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
                // The Unicode RIGHT-TO-LEFT MARK
                const char RLM = '\u200F';
                const char LRM = '\u200E'; // LEFT-TO-RIGHT MARK
                
                // Normalize line breaks first (standardize to Environment.NewLine)
                text = text.Replace("\r\n", "\n").Replace("\r", "\n");
                
                // Split by new lines to process each line separately
                var lines = text.Split('\n');
                
                for (int i = 0; i < lines.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(lines[i]))
                        continue;
                    
                    // Remove problematic invisible characters that may affect text direction
                    lines[i] = lines[i].Replace(LRM.ToString(), ""); // Remove LEFT-TO-RIGHT MARK
                    
                    // Clean up any special Unicode characters that might break word wrapping
                    lines[i] = System.Text.RegularExpressions.Regex.Replace(lines[i], @"[\u200C\u200D\u200B\uFEFF]", "");
                    
                    // Add RIGHT-TO-LEFT MARK at the beginning of each line if not already present
                    if (!lines[i].StartsWith(RLM.ToString()))
                    {
                        lines[i] = RLM + lines[i];
                    }
                    
                    // Fix common issues with Arabic punctuation and numbers
                    lines[i] = FixArabicPunctuation(lines[i]);
                }
                
                // Rejoin the lines with proper line breaks
                string result = string.Join(Environment.NewLine, lines);
                
                // Ensure the entire text starts with RTL mark for consistent display
                if (!result.StartsWith(RLM.ToString()) && !string.IsNullOrWhiteSpace(result))
                {
                    result = RLM + result;
                }
                
                // Clean up any double RTL marks that might have been added
                result = result.Replace(RLM.ToString() + RLM.ToString(), RLM.ToString());
                
                // Remove any '?' characters that might cause dictionary conflicts
                result = result.Replace("?", "");
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CleanAndFormatArabicText: {ErrorMessage}", ex.Message);
                // If there's an error, return the original text to avoid crashing
                return text;
            }
        }
    }
}
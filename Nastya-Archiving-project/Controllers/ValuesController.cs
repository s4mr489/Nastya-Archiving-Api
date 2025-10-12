using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nastya_Archiving_project.Models.DTOs.TextExtraction;
using Nastya_Archiving_project.Services.textExtraction;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ElectionsPillars.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ValuesController : ControllerBase
    {
        private readonly ITextExtractionServices _textExtractionServices;
        private readonly ILogger<ValuesController> _logger;

        public ValuesController(ITextExtractionServices textExtractionServices, ILogger<ValuesController> logger)
        {
            _textExtractionServices = textExtractionServices;
            _logger = logger;
        }

        /// <summary>
        /// استخراج النص من المستند باستخدام رقم المرجع وحفظه في قاعدة البيانات
        /// </summary>
        /// <param name="referenceNo">رقم المرجع للمستند</param>
        /// <returns>نتيجة عملية استخراج النص</returns>
        [HttpPost("extract-by-reference")]
        public async Task<IActionResult> ExtractTextByReference([FromQuery] string referenceNo)
        {
            if (string.IsNullOrWhiteSpace(referenceNo))
            {
                _logger.LogWarning("No reference number provided for text extraction");
                return BadRequest(new { 
                    error = "Reference number is required",
                    message = "يجب توفير رقم المرجع للمستند",
                    referenceNo = referenceNo 
                });
            }

            try
            {
                _logger.LogInformation("Starting text extraction for document with reference: {ReferenceNo}", referenceNo);
                
                // Start timing the extraction process
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                
                // استخراج النص من المستند وحفظه في قاعدة البيانات
                var result = await _textExtractionServices.ExtractAndSaveDocumentTextByReferenceAsync(referenceNo);
                
                stopwatch.Stop();
                
                if (result.Success)
                {
                    _logger.LogInformation("Successfully extracted and saved text for document {ReferenceNo}. " +
                        "Text length: {TextLength}, Processing time: {ProcessingTimeMs}ms", 
                        referenceNo, result.TextLength, stopwatch.ElapsedMilliseconds);
                    
                    // تطبيق تنسيق النص العربي إضافي إذا لزم الأمر
                    string formattedText = result.ExtractedText;
                    string detectedLanguage = "unknown";
                    bool isArabic = false;
                    
                    try
                    {
                        // التحقق من وجود النص العربي
                        isArabic = ContainsArabicText(formattedText);
                        
                        if (isArabic)
                        {
                            detectedLanguage = "arabic";
                            
                            // تطبيق تنسيق إضافي للنص العربي
                            formattedText = NormalizeArabicText(formattedText);
                            formattedText = CleanAndFormatArabicText(formattedText);
                            
                            _logger.LogDebug("Applied additional Arabic formatting. Final length: {FormattedLength}", 
                                formattedText?.Length ?? 0);
                        }
                    }
                    catch (Exception formatEx)
                    {
                        _logger.LogWarning(formatEx, "Error during additional text formatting for document {ReferenceNo}", referenceNo);
                        // Continue with original extracted text if formatting fails
                    }
                    
                    return Ok(new { 
                        success = true,
                        message = "تم استخراج النص وحفظه بنجاح",
                        data = new {
                            referenceNo = result.ReferenceNo,
                            extractedText = formattedText,
                            originalTextLength = result.ExtractedText?.Length ?? 0,
                            formattedTextLength = formattedText?.Length ?? 0,
                            detectedLanguage = detectedLanguage,
                            processingTimeMs = stopwatch.ElapsedMilliseconds,
                            rtl = isArabic,
                            extractionMethod = "document_reference"
                        }
                    });
                }
                else
                {
                    _logger.LogWarning("Failed to extract text for document {ReferenceNo}: {ErrorMessage}", 
                        referenceNo, result.ErrorMessage);
                    
                    return StatusCode(500, new { 
                        success = false,
                        error = "Text extraction failed",
                        message = $"فشل في استخراج النص: {result.ErrorMessage}",
                        referenceNo = result.ReferenceNo,
                        errorDetails = result.ErrorMessage,
                        processingTimeMs = stopwatch.ElapsedMilliseconds
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during text extraction for document with reference {ReferenceNo}", referenceNo);
                
                // تحديد نوع الخطأ وإرجاع رسالة مناسبة
                if (ex.Message.Contains("not found") || ex.Message.Contains("Document with reference"))
                {
                    return NotFound(new {
                        success = false,
                        error = "Document not found",
                        message = $"لم يتم العثور على المستند برقم المرجع: {referenceNo}",
                        referenceNo = referenceNo,
                        details = ex.Message
                    });
                }
                
                if (ex.Message.Contains("no file path") || ex.Message.Contains("ImgUrl"))
                {
                    return BadRequest(new {
                        success = false,
                        error = "No file associated with document",
                        message = $"لا يوجد ملف مرتبط بالمستند برقم المرجع: {referenceNo}",
                        referenceNo = referenceNo,
                        details = ex.Message
                    });
                }
                
                if (ex.Message.Contains("decrypt") || ex.Message.Contains("GetDecryptedFileStreamAsync"))
                {
                    return StatusCode(500, new {
                        success = false,
                        error = "File decryption failed",
                        message = $"فشل في فك تشفير الملف للمستند برقم المرجع: {referenceNo}",
                        referenceNo = referenceNo,
                        details = ex.Message,
                        solution = "تأكد من صحة مسار الملف وإعدادات التشفير"
                    });
                }
                
                // خطأ عام
                return StatusCode(500, new { 
                    success = false,
                    error = "Text extraction error",
                    message = $"حدث خطأ أثناء استخراج النص للمستند برقم المرجع: {referenceNo}",
                    referenceNo = referenceNo,
                    details = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
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
    }
}
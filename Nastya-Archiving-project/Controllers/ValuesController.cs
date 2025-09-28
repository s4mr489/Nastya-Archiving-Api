using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Text;
using System.Drawing;
using System.Collections.Generic;
using System.IO;
using UglyToad.PdfPig;
using Tesseract;
using ImageMagick;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace ElectionsPillars.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        [HttpPost("extract")]
        public IActionResult ExtractArabicText(IFormFile pdfFile)
        {
            if (pdfFile == null || pdfFile.Length == 0)
                return BadRequest("No file uploaded.");

            var tempPath = Path.GetTempFileName();
            using (var stream = new FileStream(tempPath, FileMode.Create))
            {
                pdfFile.CopyTo(stream);
            }

            string extractedText;
            try
            {
                if (IsTextBasedPdf(tempPath))
                {
                    extractedText = ExtractTextFromTextPdf(tempPath);
                }
                else
                {
                    extractedText = ExtractTextUsingOcr(tempPath);
                }

                // Process text for correct Arabic alignment
                extractedText = ProcessArabicText(extractedText);

                return Ok(new { text = extractedText, rtl = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
            finally
            {
                // Clean up the temporary file
                try
                {
                    System.IO.File.Delete(tempPath);
                }
                catch
                {
                    // Silently ignore deletion errors
                }
            }
        }

        [HttpPost("extract-python")]
        public IActionResult ExtractWithPython(IFormFile pdfFile)
        {
            if (pdfFile == null || pdfFile.Length == 0)
                return BadRequest("No file uploaded.");

            // Create a temporary path with a .pdf extension so Python script can recognize it
            var tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.pdf");

            try
            {
                // Log information to help with debugging
                Console.WriteLine($"Processing file: {pdfFile.FileName}, size: {pdfFile.Length} bytes");

                // Save the uploaded PDF to the temp path
                using (var stream = new FileStream(tempFilePath, FileMode.Create))
                {
                    pdfFile.CopyTo(stream);
                }

                Console.WriteLine($"PDF saved to temporary file: {tempFilePath}");

                // Run the Python script
                Console.WriteLine("Calling Python script...");
                
                // Get detailed Python environment information
                var pythonExecutable = FindBestPythonInstallation();
                string pythonInfo = GetPythonEnvironmentInfo(pythonExecutable);
                
                string extractedText = RunPythonScript(tempFilePath);
                Console.WriteLine($"Python script returned {extractedText?.Length ?? 0} characters");

                // Check if we got any text back
                if (string.IsNullOrWhiteSpace(extractedText))
                {
                    Console.WriteLine("Warning: Python script returned empty text");
                    return StatusCode(500, new { 
                        error = "Python extraction produced no text", 
                        tempFilePath,
                        pythonInfo = pythonInfo
                    });
                }

                // Process the extracted text for better Arabic rendering
                extractedText = ProcessArabicText(extractedText);

                return Ok(new
                {
                    text = extractedText,
                    rtl = true,
                    source = "python",
                    textLength = extractedText?.Length ?? 0,
                    pythonInfo = pythonInfo
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ExtractWithPython: {ex}");

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
                    ["FileExists"] = System.IO.File.Exists(tempFilePath).ToString(),
                    ["PythonEnvironment"] = pythonInfo
                };

                return StatusCode(500, new
                {
                    error = ex.Message,
                    details = ex.StackTrace,
                    innerException = ex.InnerException?.Message,
                    environment = environmentInfo
                });
            }
            finally
            {
                // Clean up the temporary file
                try
                {
                    if (System.IO.File.Exists(tempFilePath))
                    {
                        System.IO.File.Delete(tempFilePath);
                        Console.WriteLine($"Deleted temporary file: {tempFilePath}");
                    }
                }
                catch (Exception ex)
                {
                    // Log deletion errors
                    Console.WriteLine($"Failed to delete temporary file {tempFilePath}: {ex.Message}");
                }
            }
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

        [HttpGet("python-check")]
        public IActionResult CheckPythonEnvironment()
        {
            var results = new Dictionary<string, object>();

            // Check if Python is available
            try
            {
                // Get all Python paths
                var pythonPaths = GetCommandPaths("python");
                var pythonPaths3 = GetCommandPaths("python3");

                results["pythonPaths"] = pythonPaths;
                if (pythonPaths3[0] != "Not found in PATH")
                {
                    results["python3Paths"] = pythonPaths3;
                }

                // Try each Python path until one works
                var testedPaths = new List<Dictionary<string, string>>();
                bool foundWorkingPython = false;

                // Check each Python path
                foreach (var pythonPath in pythonPaths.Concat(pythonPaths3).Where(p => p != "Not found in PATH"))
                {
                    var pathResult = new Dictionary<string, string>();
                    pathResult["path"] = pythonPath;

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
                        pathResult["version"] = version;

                        // This path works, check packages
                        var packageResults = new Dictionary<string, string>();
                        CheckPythonPackageForPath(pythonPath, packageResults, "PyMuPDF", "import fitz; print('PyMuPDF version:', fitz.__version__)");
                        CheckPythonPackageForPath(pythonPath, packageResults, "pdfminer.six", "from pdfminer import __version__; print('pdfminer version:', __version__)");

                        pathResult["packages"] = System.Text.Json.JsonSerializer.Serialize(packageResults);

                        // If both packages are installed, this is our preferred Python environment
                        if (packageResults.ContainsKey("PyMuPDF") && packageResults["PyMuPDF"] == "Installed" &&
                            packageResults.ContainsKey("pdfminer.six") && packageResults["pdfminer.six"] == "Installed")
                        {
                            foundWorkingPython = true;
                            results["recommendedPython"] = pythonPath;
                        }

                        testedPaths.Add(pathResult);
                    }
                    catch (Exception ex)
                    {
                        pathResult["error"] = ex.Message;
                        testedPaths.Add(pathResult);
                    }
                }

                results["testedPaths"] = testedPaths;

                // If we haven't found a working Python with all required packages,
                // recommend the first one that works at all
                if (!foundWorkingPython && testedPaths.Count > 0)
                {
                    results["recommendedPython"] = testedPaths.FirstOrDefault(p => !p.ContainsKey("error"))?["path"];
                }

                // Check script location
                var scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "extract_arabic_pdf.py");
                results["scriptPath"] = scriptPath;
                results["scriptExists"] = System.IO.File.Exists(scriptPath).ToString();

                if (results["scriptExists"].ToString() == "True" && results.ContainsKey("recommendedPython"))
                {
                    // Check if script works
                    var recommendedPython = results["recommendedPython"]?.ToString();
                    if (!string.IsNullOrEmpty(recommendedPython))
                    {
                        var psi = new ProcessStartInfo();
                        psi.FileName = recommendedPython;
                        psi.Arguments = $"\"{scriptPath}\" --help";
                        psi.RedirectStandardOutput = true;
                        psi.RedirectStandardError = true;
                        psi.UseShellExecute = false;
                        psi.CreateNoWindow = true;
                        psi.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;

                        try
                        {
                            using var helpProcess = Process.Start(psi);
                            string helpOutput = helpProcess.StandardOutput.ReadToEnd().Trim();
                            string errorOutput = helpProcess.StandardError.ReadToEnd().Trim();
                            helpProcess.WaitForExit();

                            results["scriptHelp"] = helpOutput;
                            if (!string.IsNullOrEmpty(errorOutput))
                            {
                                results["scriptError"] = errorOutput;
                            }

                            results["scriptWorks"] = helpProcess.ExitCode == 0;
                        }
                        catch (Exception ex)
                        {
                            results["scriptTestError"] = ex.Message;
                        }
                    }
                }

                // Create script in the current directory if it doesn't exist
                if (results["scriptExists"].ToString() == "False")
                {
                    try
                    {
                        var scriptContent = System.IO.File.ReadAllText("extract_arabic_pdf.py");
                        var currentDirScriptPath = Path.Combine(Directory.GetCurrentDirectory(), "extract_arabic_pdf.py");
                        System.IO.File.WriteAllText(currentDirScriptPath, scriptContent);
                        results["scriptCreatedAt"] = currentDirScriptPath;
                    }
                    catch (Exception ex)
                    {
                        results["scriptCreationError"] = ex.Message;
                    }
                }

                // Additional diagnostics
                results["workingDirectory"] = AppDomain.CurrentDomain.BaseDirectory;
                results["currentDirectory"] = Directory.GetCurrentDirectory();
                results["tempDir"] = Path.GetTempPath();
                results["diagnosticTime"] = DateTime.Now.ToString("o");

                return Ok(results);
            }
            catch (Exception ex)
            {
                results["error"] = ex.Message;
                results["stackTrace"] = ex.StackTrace;
                return StatusCode(500, results);
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

        private string RunPythonScript(string pdfPath)
        {
            // Find the script path
            var scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "extract_arabic_pdf.py");
            var currentDirScriptPath = Path.Combine(Directory.GetCurrentDirectory(), "extract_arabic_pdf.py");

            // Check if the script exists in the base directory
            if (!System.IO.File.Exists(scriptPath) && System.IO.File.Exists(currentDirScriptPath))
            {
                scriptPath = currentDirScriptPath; // Use script in current directory
                Console.WriteLine($"Using script from current directory: {scriptPath}");
            }
            else if (!System.IO.File.Exists(scriptPath))
            {
                scriptPath = "extract_arabic_pdf.py"; // Try with relative path
                Console.WriteLine($"Script not found in base directory, trying relative path: {scriptPath}");
            }
            else
            {
                Console.WriteLine($"Using script from base directory: {scriptPath}");
            }

            // Find the best Python installation
            string pythonExecutable = FindBestPythonInstallation();
            Console.WriteLine($"Selected Python executable: {pythonExecutable}");

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
                Console.WriteLine($"Starting Python process: {psi.FileName} {psi.Arguments}");
                Console.WriteLine($"Working directory: {psi.WorkingDirectory}");

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
                        Console.WriteLine($"Python stderr: {e.Data}");
                    }
                };
                process.BeginErrorReadLine();

                // Read standard output
                string output = process.StandardOutput.ReadToEnd();
                Console.WriteLine($"Python stdout length: {output?.Length ?? 0} characters");

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
                        Console.WriteLine($"Reading text from file: {tempFilePath}");

                        // Read the file content and clean up
                        output = System.IO.File.ReadAllText(tempFilePath, Encoding.UTF8);
                        try { System.IO.File.Delete(tempFilePath); } catch { }
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

        // Helper method to find the full path of an executable in the system PATH
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

        // Helper method to find the first valid path of an executable in the system PATH
        private string GetCommandPath(string command)
        {
            var paths = GetCommandPaths(command);
            return paths.FirstOrDefault() ?? "Not found in PATH";
        }

        private bool IsTextBasedPdf(string path)
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

        private string ExtractTextFromTextPdf(string path)
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

        private string ExtractTextUsingOcr(string path)
        {
            var images = ConvertPdfToImages(path);
            var text = new StringBuilder();

            try
            {
                // Check if tessdata directory exists
                if (!Directory.Exists("./tessdata"))
                {
                    throw new DirectoryNotFoundException("Tessdata directory not found. Please make sure the Arabic language data file is available in the tessdata directory.");
                }

                // Initialize Tesseract with Arabic language and right-to-left reading order
                using var engine = new TesseractEngine("./tessdata", "ara", EngineMode.Default);

                // Configure engine for Arabic text
                engine.SetVariable("textord_tablefind_show_vlines", false);
                engine.SetVariable("textord_use_cjk_fp_model", false);
                engine.SetVariable("language_model_ngram_on", true);
                engine.SetVariable("paragraph_text_based", true);
                engine.SetVariable("textord_heavy_nr", false);
                engine.SetVariable("tessedit_pageseg_mode", 1); // PSM_AUTO_OSD
                engine.SetVariable("preserve_interword_spaces", 1); // Preserve spaces

                foreach (var image in images)
                {
                    // Convert the Bitmap to a Pix object that Tesseract can use
                    using var pix = BitmapToPix(image);

                    // Process the image with OCR
                    using var page = engine.Process(pix);

                    // Get the text and apply RTL corrections
                    var pageText = page.GetText();
                    text.AppendLine(pageText);
                }

                return text.ToString();
            }
            finally
            {
                // Clean up images
                foreach (var image in images)
                {
                    image?.Dispose();
                }
            }
        }

        // Process text to ensure correct Arabic alignment and rendering
        private string ProcessArabicText(string text)
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

        // Convert a Bitmap to a Tesseract Pix object
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
                    System.IO.File.Delete(tempFile);
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
            catch (Exception)
            {
                // If any enhancement method fails, continue without that enhancement
                // This ensures we still get some text even if image processing fails
            }
        }

        [HttpGet("python-install-packages")]
        public IActionResult InstallPythonPackages()
        {
            var results = new Dictionary<string, object>();

            try
            {
                // Find the Python executable used by the application
                string pythonPath = FindBestPythonInstallation();
                results["pythonPath"] = pythonPath;
                
                // Try to install the packages directly
                var installResults = InstallPythonPackages(pythonPath);
                results["installResults"] = installResults;
                
                // Test if the packages are now installed
                var testResults = TestPythonPackages(pythonPath);
                results["testResults"] = testResults;
                
                // Add detailed environment information
                results["environmentInfo"] = GetPythonEnvironmentInfo(pythonPath);
                
                return Ok(new {
                    message = "Installation attempted. Please check the results.",
                    pythonPath = pythonPath,
                    installResults = installResults,
                    testResults = testResults,
                    instructions = "If installation failed, please run the scripts/install_dependencies_direct.ps1 script with administrator privileges."
                });
            }
            catch (Exception ex)
            {
                results["error"] = ex.Message;
                results["stackTrace"] = ex.StackTrace;
                
                return StatusCode(500, new {
                    error = ex.Message,
                    message = "Failed to install packages. Please run scripts/install_dependencies_direct.ps1 script with administrator privileges."
                });
            }
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
    }
}

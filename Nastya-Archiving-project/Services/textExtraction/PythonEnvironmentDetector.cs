using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Nastya_Archiving_project.Services.textExtraction
{
    /// <summary>
    /// Helper class to detect and validate Python environment
    /// </summary>
    public class PythonEnvironmentDetector
    {
        private readonly ILogger _logger;

        public PythonEnvironmentDetector(ILogger<PythonEnvironmentDetector> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Checks if Python environment is properly configured
        /// </summary>
        /// <returns>True if Python is installed with required packages</returns>
        public async Task<bool> IsPythonEnvironmentConfigured()
        {
            try
            {
                // Try to use cached environment info first
                string envFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "python_env_info.json");
                if (System.IO.File.Exists(envFilePath))
                {
                    try
                    {
                        string json = await System.IO.File.ReadAllTextAsync(envFilePath);
                        var envInfo = JsonSerializer.Deserialize<PythonEnvironmentInfo>(json);
                        
                        // Verify the cached Python path still works
                        if (!string.IsNullOrEmpty(envInfo.PythonPath) && 
                            System.IO.File.Exists(envInfo.PythonPath) &&
                            envInfo.PyMuPDFInstalled &&
                            envInfo.PdfminerInstalled)
                        {
                            // Perform a quick check that the Python executable works
                            bool pythonWorks = await TestPythonExecutable(envInfo.PythonPath);
                            if (pythonWorks)
                            {
                                _logger.LogInformation("Using cached Python environment: {PythonPath}", envInfo.PythonPath);
                                return true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to read Python environment cache");
                        // Continue with live detection
                    }
                }

                // Perform live detection
                string pythonPath = await FindPythonExecutable();
                if (string.IsNullOrEmpty(pythonPath))
                {
                    _logger.LogWarning("Python not found in system PATH or common locations");
                    return false;
                }

                // Check required packages
                bool hasPyMuPDF = await TestPythonPackage(pythonPath, "fitz");
                bool hasPdfMiner = await TestPythonPackage(pythonPath, "pdfminer");

                bool isConfigured = hasPyMuPDF && hasPdfMiner;

                _logger.LogInformation(
                    "Python environment check: Path={PythonPath}, PyMuPDF={PyMuPDF}, pdfminer.six={PdfMiner}",
                    pythonPath, hasPyMuPDF, hasPdfMiner);

                // Update the environment info cache
                if (isConfigured)
                {
                    var envInfo = new PythonEnvironmentInfo
                    {
                        PythonPath = pythonPath,
                        PythonVersion = await GetPythonVersion(pythonPath),
                        PyMuPDFInstalled = hasPyMuPDF,
                        PdfminerInstalled = hasPdfMiner,
                        InstallationDate = DateTime.UtcNow.ToString("o")
                    };

                    string json = JsonSerializer.Serialize(envInfo, new JsonSerializerOptions { WriteIndented = true });
                    
                    try
                    {
                        await System.IO.File.WriteAllTextAsync(envFilePath, json);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to save Python environment cache");
                    }
                }

                return isConfigured;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking Python environment");
                return false;
            }
        }

        /// <summary>
        /// Find Python executable in common locations
        /// </summary>
        private async Task<string> FindPythonExecutable()
        {
            string[] possiblePaths = {
                "python",
                "python3",
                @"C:\Python39\python.exe",
                @"C:\Python310\python.exe",
                @"C:\Python311\python.exe",
                @"C:\Python312\python.exe",
                @"C:\Program Files\Python39\python.exe",
                @"C:\Program Files\Python310\python.exe",
                @"C:\Program Files\Python311\python.exe",
                @"C:\Program Files\Python312\python.exe",
                @"C:\Program Files (x86)\Python39\python.exe",
                @"C:\Program Files (x86)\Python310\python.exe",
                @"C:\Program Files (x86)\Python311\python.exe",
                @"C:\Program Files (x86)\Python312\python.exe"
            };

            foreach (string path in possiblePaths)
            {
                bool works = await TestPythonExecutable(path);
                if (works)
                {
                    return path;
                }
            }

            return null;
        }

        /// <summary>
        /// Test if Python executable works
        /// </summary>
        private async Task<bool> TestPythonExecutable(string pythonPath)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = pythonPath,
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process == null)
                    return false;

                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                
                await process.WaitForExitAsync();
                
                return process.ExitCode == 0 && (output + error).Contains("Python");
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Test if a Python package is installed
        /// </summary>
        private async Task<bool> TestPythonPackage(string pythonPath, string packageName)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = pythonPath,
                    Arguments = $"-c \"import {packageName}; print('OK')\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process == null)
                    return false;

                string output = await process.StandardOutput.ReadToEndAsync();
                
                await process.WaitForExitAsync();
                
                return process.ExitCode == 0 && output.Trim() == "OK";
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get Python version
        /// </summary>
        private async Task<string> GetPythonVersion(string pythonPath)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = pythonPath,
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process == null)
                    return "Unknown";

                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                
                await process.WaitForExitAsync();
                
                return (output + error).Trim();
            }
            catch
            {
                return "Unknown";
            }
        }

        /// <summary>
        /// Internal class to store Python environment info
        /// </summary>
        private class PythonEnvironmentInfo
        {
            public string PythonPath { get; set; }
            public string PythonVersion { get; set; }
            public bool PyMuPDFInstalled { get; set; }
            public bool PdfminerInstalled { get; set; }
            public string InstallationDate { get; set; }
        }
    }
}
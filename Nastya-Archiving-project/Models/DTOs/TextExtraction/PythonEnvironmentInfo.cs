using System.Collections.Generic;

namespace Nastya_Archiving_project.Models.DTOs.TextExtraction
{
    /// <summary>
    /// Contains information about the Python environment detected on the system
    /// </summary>
    public class PythonEnvironmentInfo
    {
        /// <summary>
        /// Path to the Python executable
        /// </summary>
        public string PythonPath { get; set; }

        /// <summary>
        /// Python version information
        /// </summary>
        public string PythonVersion { get; set; }

        /// <summary>
        /// Dictionary of tested Python paths and their versions
        /// </summary>
        public Dictionary<string, string> TestedPaths { get; set; }

        /// <summary>
        /// Information about installed packages relevant for PDF text extraction
        /// </summary>
        public Dictionary<string, string> Packages { get; set; }

        /// <summary>
        /// Path to the Python script used for extraction
        /// </summary>
        public string ScriptPath { get; set; }

        /// <summary>
        /// Indicates if the extraction script exists at the specified path
        /// </summary>
        public bool ScriptExists { get; set; }

        /// <summary>
        /// Current working directory of the application
        /// </summary>
        public string WorkingDirectory { get; set; }

        /// <summary>
        /// Current directory according to Directory.GetCurrentDirectory()
        /// </summary>
        public string CurrentDirectory { get; set; }

        /// <summary>
        /// Error message if the environment check failed
        /// </summary>
        public string Error { get; set; }
    }
}
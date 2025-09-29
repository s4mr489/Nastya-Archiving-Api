using System.Collections.Generic;

namespace Nastya_Archiving_project.Models.DTOs.TextExtraction
{
    /// <summary>
    /// Result of attempting to install Python packages required for PDF text extraction
    /// </summary>
    public class PythonPackageInstallationResult
    {
        /// <summary>
        /// Path to the Python executable used for the installation
        /// </summary>
        public string PythonPath { get; set; }

        /// <summary>
        /// Results of the installation attempts for each package
        /// </summary>
        public Dictionary<string, string> InstallResults { get; set; }

        /// <summary>
        /// Results of testing if packages are correctly installed after installation attempts
        /// </summary>
        public Dictionary<string, string> TestResults { get; set; }

        /// <summary>
        /// Detailed environment information after the installation
        /// </summary>
        public string EnvironmentInfo { get; set; }

        /// <summary>
        /// Error message if the installation process failed
        /// </summary>
        public string Error { get; set; }
    }
}
using System;

namespace Nastya_Archiving_project.Models.DTOs.Limitation
{
    /// <summary>
    /// Data Transfer Object for license creation/update parameters
    /// </summary>
    public class LicenseCreationDTO
    {
        /// <summary>
        /// Admin username for authentication
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Admin password for authentication
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Custom license key (optional)
        /// </summary>
        public string CustomLicenseKey { get; set; }

        /// <summary>
        /// Expiration date for the license
        /// </summary>
        public DateTime ExpirationDate { get; set; }

        /// <summary>
        /// Maximum number of users allowed
        /// </summary>
        public int? MaxUsers { get; set; }

        /// <summary>
        /// Total storage limit in GB
        /// </summary>
        public int? MaxStorageGB { get; set; }

        /// <summary>
        /// System version string
        /// </summary>
        public string SystemVersion { get; set; }

        /// <summary>
        /// Current total storage in MB (optional)
        /// </summary>
        public decimal? TotalStorageMB { get; set; }
    }
}
using DocumentFormat.OpenXml.Office.Y2022.FeaturePropertyBag;
using Microsoft.EntityFrameworkCore;
using Nastya_Archiving_project.Data;
using Nastya_Archiving_project.Models.DTOs;
using Nastya_Archiving_project.Models.DTOs.Limitation;
using Nastya_Archiving_project.Services.encrpytion;
using Nastya_Archiving_project.Services.SystemInfo;
using System.Text;

namespace Nastya_Archiving_project.Services.Limitation
{
    public class LimitaionServices : ILimitationServices
    {
        private readonly AppDbContext _context;
        private readonly IEncryptionServices _encryptionServices;
        private readonly ISystemInfoServices _systemInfoServices;
        
        public LimitaionServices(
            AppDbContext context,
            IEncryptionServices encryptionServices,
            ISystemInfoServices systemInfoServices
            )
        {
            _context = context;
            _encryptionServices = encryptionServices;
            _systemInfoServices = systemInfoServices;
        }

        /// <summary>
        /// Creates or updates a text file with encrypted field values for system limitation information
        /// </summary>
        /// <param name="licenseParams">Parameters for license creation/update</param>
        /// <returns>Response with status and file path information</returns>
        public async Task<BaseResponseDTOs> CreateEncryptedTextFile(LicenseCreationDTO licenseParams)
        {
            // Validate credentials
            if (licenseParams == null)
                return new BaseResponseDTOs(null, 400, "License parameters cannot be null");

            if (string.IsNullOrEmpty(licenseParams.Username) || string.IsNullOrEmpty(licenseParams.Password))
                return new BaseResponseDTOs(null, 400, "Username and password are required");

            if (licenseParams.Username != "nastya" || licenseParams.Password != "nastya")
                return new BaseResponseDTOs(null, 400, "Username or password is wrong");

            try
            {
                // Define the path in wwwroot folder
                string outputPath = Path.Combine("wwwroot", "licenses", "Limit.txt");

                // Ensure the directory exists
                string directory = Path.GetDirectoryName(outputPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Ensure the file has .txt extension
                string finalOutputPath = Path.ChangeExtension(outputPath, "txt");

                // Dictionary to store values from existing file (if any)
                Dictionary<string, string> existingValues = new Dictionary<string, string>();
                string existingLicenseKey = null;

                // Check if file exists and read existing values
                if (File.Exists(finalOutputPath))
                {
                    var existingLicenseResponse = await ReadEncryptedTextFile();
                    if (existingLicenseResponse.StatusCode == 200 && existingLicenseResponse.Data != null)
                    {
                        dynamic responseData = existingLicenseResponse.Data;
                        if (responseData.License != null)
                        {
                            // Store existing license key for signature validation
                            existingLicenseKey = responseData.License.LicenseKey;

                            // Get all decrypted values
                            var rawData = ((IDictionary<string, object>)responseData).ContainsKey("Debug")
                                ? (IDictionary<string, object>)responseData.Debug.RawDecryptedData
                                : new Dictionary<string, object>();

                            // Store existing values in dictionary for later use
                            foreach (var kvp in rawData)
                            {
                                existingValues[kvp.Key] = kvp.Value?.ToString();
                            }
                        }
                    }
                }

                try
                {
                    // Get current date for creation timestamp
                    var currentDate = DateTime.Now;

                    // Use provided expiration date directly from params
                    DateTime limitationDate = licenseParams.ExpirationDate;

                    // Determine license key to use
                    string licenseKey;
                    if (!string.IsNullOrEmpty(licenseParams.CustomLicenseKey))
                    {
                        // Use custom license key if provided
                        licenseKey = licenseParams.CustomLicenseKey;
                    }
                    else if (!string.IsNullOrEmpty(existingLicenseKey))
                    {
                        // Use existing license key if available
                        licenseKey = existingLicenseKey;
                    }
                    else
                    {
                        // Generate new license key
                        licenseKey = Guid.NewGuid().ToString("N");
                    }

                    // Use max users value from params or existing license
                    string maxUsersValue = licenseParams.MaxUsers?.ToString() ??
                        (existingValues.TryGetValue("NASTYA-ARCHIVING-LICENSE.MaxUsers", out string existingMaxUsers)
                            ? existingMaxUsers
                            : "100");

                    // Create a dictionary of values to encrypt - only using user-provided values
                    var licenseValues = new Dictionary<string, string>
                    {
                        { "LicenseKey", licenseKey },
                        { "CreationDate", currentDate.ToString("yyyy-MM-dd HH:mm:ss") },
                        { "ExpirationDate", limitationDate.ToString("yyyy-MM-dd HH:mm:ss") },
                        { "MaxUsers", maxUsersValue },
                        { "TotalStorageMB", licenseParams.TotalStorageMB?.ToString() ?? "0" },
                        { "MaxStorageGB", licenseParams.MaxStorageGB?.ToString() ??
                            (existingValues.TryGetValue("NASTYA-ARCHIVING-LICENSE.MaxStorageGB", out string existingMaxStorage)
                                ? existingMaxStorage
                                : "100") },
                        { "SystemVersion", !string.IsNullOrEmpty(licenseParams.SystemVersion)
                            ? licenseParams.SystemVersion
                            : (existingValues.TryGetValue("NASTYA-ARCHIVING-LICENSE.SystemVersion", out string existingVersion)
                                ? existingVersion
                                : "1.0.0") },
                    };
                    // Encrypt each value individually
                    var encryptedValues = new Dictionary<string, string>();
                    foreach (var entry in licenseValues)
                    {
                        encryptedValues[entry.Key] = _encryptionServices.EncryptString256Bit(entry.Value);
                    }

                    // Create the license data as text with encrypted values
                    StringBuilder licenseText = new StringBuilder();
                    licenseText.AppendLine("[NASTYA-ARCHIVING-LICENSE]");
                    licenseText.AppendLine($"LicenseKey={encryptedValues["LicenseKey"]}");
                    licenseText.AppendLine($"CreationDate={encryptedValues["CreationDate"]}");
                    licenseText.AppendLine($"ExpirationDate={encryptedValues["ExpirationDate"]}");
                    licenseText.AppendLine($"MaxUsers={encryptedValues["MaxUsers"]}");
                    licenseText.AppendLine($"TotalStorageMB={encryptedValues["TotalStorageMB"]}");
                    licenseText.AppendLine($"MaxStorageGB={encryptedValues["MaxStorageGB"]}");
                    licenseText.AppendLine($"SystemVersion={encryptedValues["SystemVersion"]}");

                    // Add validation hash for integrity verification
                    // The hash is created from the original (unencrypted) values
                    string licenseDataStr = $"{licenseKey}|{limitationDate:yyyy-MM-dd}|{maxUsersValue}";
                    string signature = _encryptionServices.ComputeMD5Hash(licenseDataStr);

                    // Encrypt the signature
                    string encryptedSignature = _encryptionServices.EncryptString256Bit(signature);

                    licenseText.AppendLine();
                    licenseText.AppendLine("[SIGNATURE]");
                    licenseText.AppendLine(encryptedSignature);

                    // Write the content to the text file
                    await File.WriteAllTextAsync(finalOutputPath, licenseText.ToString());

                    // Determine if this was an update or creation
                    string operationType = File.Exists(finalOutputPath) ? "updated" : "created";

                    // Return success response
                    return new BaseResponseDTOs(
                        new
                        {
                            FilePath = finalOutputPath,
                            CreationDate = currentDate,
                            ExpirationDate = limitationDate,
                            LicenseKey = licenseKey,
                            IsEncrypted = true,
                            MaxUsers = maxUsersValue,
                            MaxStorageGB = licenseParams.MaxStorageGB ?? 100,
                            TotalStorageMB = licenseParams.TotalStorageMB ?? 0,
                            SystemVersion = licenseParams.SystemVersion ?? "1.0.0",
                            Operation = operationType
                        },
                        200,
                        null
                    );
                }
                catch (Exception ex)
                {
                    return new BaseResponseDTOs(
                        null,
                        500,
                        $"Inner error: {ex.Message}"
                    );
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions and return error
                return new BaseResponseDTOs(
                    null,
                    500,
                    $"Error creating or updating encrypted text file: {ex.Message}"
                );
            }
        }
        /// <summary>
        /// Reads and decrypts a text file containing system license information with encrypted values
        /// and formats it as a standardized license response
        /// </summary>
        /// <param name="filePath">Optional path to the text file with encrypted values. If null, defaults to wwwroot/licenses/Limit.txt</param>
        /// <returns>Decrypted license information formatted as a system response</returns>
        public async Task<BaseResponseDTOs> ReadEncryptedTextFile()
        {
            try
            {
                // If no specific path is provided, use the default path in wwwroot
                string actualFilePath =Path.Combine("wwwroot", "licenses", "Limit.txt");

                if (!File.Exists(actualFilePath))
                {
                    return new BaseResponseDTOs(
                        null,
                        404,
                        $"License file not found: {actualFilePath}"
                    );
                }

                // Read the content
                string fileContent = await File.ReadAllTextAsync(actualFilePath);

                // Parse the text file
                var licenseData = new Dictionary<string, string>();
                var decryptedData = new Dictionary<string, string>();
                string currentSection = null;

                using (StringReader reader = new StringReader(fileContent))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        // Skip empty lines
                        if (string.IsNullOrWhiteSpace(line))
                            continue;

                        // Check if this is a section header
                        if (line.StartsWith("[") && line.EndsWith("]"))
                        {
                            currentSection = line.Trim('[', ']');
                            continue;
                        }

                        // Parse key-value pairs
                        int equalsIndex = line.IndexOf('=');
                        if (equalsIndex > 0)
                        {
                            string key = line.Substring(0, equalsIndex).Trim();
                            string encryptedValue = line.Substring(equalsIndex + 1).Trim();

                            // Store with section prefix for clarity
                            string fullKey = currentSection != null ? $"{currentSection}.{key}" : key;
                            licenseData[fullKey] = encryptedValue;

                            // Decrypt the value
                            try
                            {
                                string decryptedValue = _encryptionServices.DecryptString256Bit(encryptedValue);
                                decryptedData[fullKey] = decryptedValue;
                            }
                            catch
                            {
                                // If decryption fails, store the original value
                                decryptedData[fullKey] = "***DECRYPTION_FAILED***";
                            }
                        }
                        else if (currentSection == "SIGNATURE" && !string.IsNullOrWhiteSpace(line))
                        {
                            // For signature, store and decrypt
                            licenseData["Signature"] = line.Trim();
                            try
                            {
                                decryptedData["Signature"] = _encryptionServices.DecryptString256Bit(line.Trim());
                            }
                            catch
                            {
                                decryptedData["Signature"] = "***DECRYPTION_FAILED***";
                            }
                        }
                    }
                }

                // Verify the signature if available
                bool isValid = false;
                if (decryptedData.TryGetValue("NASTYA-ARCHIVING-LICENSE.LicenseKey", out string licenseKey) &&
                    decryptedData.TryGetValue("NASTYA-ARCHIVING-LICENSE.ExpirationDate", out string expirationDate) &&
                    decryptedData.TryGetValue("NASTYA-ARCHIVING-LICENSE.MaxUsers", out string maxUsers) &&
                    decryptedData.TryGetValue("Signature", out string signature))
                {
                    try
                    {
                        DateTime expDateValue = DateTime.Parse(expirationDate);
                        string licenseDataStr = $"{licenseKey}|{expDateValue:yyyy-MM-dd}|{maxUsers}";
                        isValid = _encryptionServices.VerifyHash(licenseDataStr, signature);
                    }
                    catch
                    {
                        isValid = false;
                    }
                }

                // Format the license information for response
                var licenseInfo = new
                {
                    LicenseKey = decryptedData.TryGetValue("NASTYA-ARCHIVING-LICENSE.LicenseKey", out string lKey) ? lKey : null,
                    LicenseStatus = isValid ? "Valid" : "Invalid",
                    CreationDate = decryptedData.TryGetValue("NASTYA-ARCHIVING-LICENSE.CreationDate", out string createDate)
                        ? DateTime.TryParse(createDate, out DateTime cDate) ? (DateTime?)cDate : null
                        : null,
                    ExpirationDate = decryptedData.TryGetValue("NASTYA-ARCHIVING-LICENSE.ExpirationDate", out string expDate)
                        ? DateTime.TryParse(expDate, out DateTime eDate) ? (DateTime?)eDate : null
                        : null,
                    SystemLimits = new
                    {
                        MaxUsers = decryptedData.TryGetValue("NASTYA-ARCHIVING-LICENSE.MaxUsers", out string maxU)
                            ? int.TryParse(maxU, out int mUsers) ? mUsers : 0
                            : 0,
                        MaxStorageGB = decryptedData.TryGetValue("NASTYA-ARCHIVING-LICENSE.MaxStorageGB", out string maxS)
                            ? int.TryParse(maxS, out int mStorage) ? mStorage : 0
                            : 0,
                        CurrentStorageMB = decryptedData.TryGetValue("NASTYA-ARCHIVING-LICENSE.TotalStorageMB", out string curS)
                            ? decimal.TryParse(curS, out decimal cStorage) ? cStorage : 0
                            : 0
                    },
                    SystemInfo = new
                    {
                        SystemVersion = decryptedData.TryGetValue("NASTYA-ARCHIVING-LICENSE.SystemVersion", out string sysV)
                            ? sysV
                            : "Unknown",
                        CreatedBy = decryptedData.TryGetValue("NASTYA-ARCHIVING-LICENSE.CreatedBy", out string createdB)
                            ? createdB
                            : "Unknown"
                    },
                    LicenseValidation = new
                    {
                        IsValid = isValid,
                        SignatureValid = isValid,
                        DecryptionSuccessful = decryptedData.Count > 0,
                        FilePath = actualFilePath,
                        // Fix the variable name conflict in LicenseValidation.IsExpired
                        IsExpired = GetDateFromDecryptedData(decryptedData, "NASTYA-ARCHIVING-LICENSE.ExpirationDate") is DateTime expirationDateTime
                    ? expirationDateTime < DateTime.Now
                    : true
                    }
                };

                // For debugging or administrative purposes, you might want to include the raw data
                // but we hide it from normal responses
                var debugInfo = new Dictionary<string, object>
                {
                    { "RawDecryptedData", decryptedData },
                    { "RawEncryptedData", licenseData }
                };

                // Return the formatted license information
                return new BaseResponseDTOs(
                    new
                    {
                        License = licenseInfo,
                        // Uncomment the line below if you want to include debug info in the response
                        Debug = debugInfo
                    },
                    200,
                    null
                );
            }
            catch (Exception ex)
            {
                return new BaseResponseDTOs(
                    null,
                    500,
                    $"Error reading encrypted text file: {ex.Message}"
                );
            }
        }


        public async Task<BaseResponseDTOs> GetSmartSearchStatus()
        {
            var smartSearch = await _context.PPackegs.FirstOrDefaultAsync();

            return new BaseResponseDTOs(
                new
                {
                    IsSmartSearchEnabled = smartSearch?.status == 1 ? "true" : "false",
                },
                200,
                null
            );
        }
        /// <summary>
        /// Sets the smart search functionality to enabled (true)
        /// </summary>
        /// <returns>Response with status information</returns>
        public async Task<BaseResponseDTOs> EnableSmartSearch()
        {
            try
            {
                var smartSearch = await _context.PPackegs.FirstOrDefaultAsync();

                if (smartSearch == null)
                {
                    // If no record exists, create one with status = 1 (enabled)
                    smartSearch = new Models.PPackeg
                    {
                        Dscrp = "Smart Search Feature",
                        status = 1
                    };

                    _context.PPackegs.Add(smartSearch);
                }
                else
                {
                    // Update existing record to enabled
                    smartSearch.status = 1;
                    _context.PPackegs.Update(smartSearch);
                }

                await _context.SaveChangesAsync();

                return new BaseResponseDTOs(
                    new
                    {
                        IsSmartSearchEnabled = "true",
                        Message = "Smart search has been successfully enabled"
                    },
                    200,
                    null
                );
            }
            catch (Exception ex)
            {
                return new BaseResponseDTOs(
                    null,
                    500,
                    $"Error enabling smart search: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Sets the smart search functionality to disabled (false)
        /// </summary>
        /// <returns>Response with status information</returns>
        public async Task<BaseResponseDTOs> DisableSmartSearch()
        {
            try
            {
                var smartSearch = await _context.PPackegs.FirstOrDefaultAsync();

                if (smartSearch == null)
                {
                    // If no record exists, create one with status = 0 (disabled)
                    smartSearch = new Models.PPackeg
                    {
                        Dscrp = "Smart Search Feature",
                        status = 0
                    };

                    _context.PPackegs.Add(smartSearch);
                }
                else
                {
                    // Update existing record to disabled
                    smartSearch.status = 0;
                    _context.PPackegs.Update(smartSearch);
                }

                await _context.SaveChangesAsync();

                return new BaseResponseDTOs(
                    new
                    {
                        IsSmartSearchEnabled = "false",
                        Message = "Smart search has been successfully disabled"
                    },
                    200,
                    null
                );
            }
            catch (Exception ex)
            {
                return new BaseResponseDTOs(
                    null,
                    500,
                    $"Error disabling smart search: {ex.Message}"
                );
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        // Then add this helper method in the class
        private DateTime? GetDateFromDecryptedData(Dictionary<string, string> data, string key)
        {
            if (data.TryGetValue(key, out string dateStr) && DateTime.TryParse(dateStr, out DateTime date))
            {
                return date;
            }
            return null;
        }
    }
}

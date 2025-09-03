using Microsoft.EntityFrameworkCore;
using Nastya_Archiving_project.Data;
using Nastya_Archiving_project.Helper;
using Nastya_Archiving_project.Models.DTOs;
using Nastya_Archiving_project.Services.encrpytion;
using Nastya_Archiving_project.Services.SystemInfo;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Xml;

namespace Nastya_Archiving_project.Services.home
{
    public class HomeServices : IHomeServices
    {
        private readonly AppDbContext _context;
        private readonly ISystemInfoServices _systemInfoServices;
        private readonly IEncryptionServices _encryptionServices;

        public HomeServices(AppDbContext context, ISystemInfoServices systemInfoServices, IEncryptionServices encryptionServices)
        {
            _context = context;
            _systemInfoServices = systemInfoServices;
            _encryptionServices = encryptionServices;
        }

        public async Task<BaseResponseDTOs> ActiveUsers()
        {
            try
            {
                // Get active users from document editing activity within the last 30 days
                var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
                
                // Query the Users_Editing table which contains login and document activity
                var activeUsersQuery = await _context.UsersEditings
                    .Where(ue => ue.EditDate.HasValue && ue.EditDate.Value >= thirtyDaysAgo)
                    .GroupBy(ue => ue.Editor)
                    .Select(g => new
                    {
                        UserName = g.Key,
                        LastActivity = g.Max(ue => ue.EditDate),
                        ActivityCount = g.Count(),
                        Operations = g.Select(ue => ue.OperationType).Distinct().ToList()
                    })
                    .OrderByDescending(a => a.LastActivity)
                    .ToListAsync();

                // As a backup, also query archiving documents for editors
                var documentEditorsQuery = await _context.ArcivingDocs
                    .Where(d => !string.IsNullOrEmpty(d.Editor) && d.EditDate.HasValue && d.EditDate.Value >= thirtyDaysAgo)
                    .GroupBy(d => d.Editor)
                    .Select(g => new 
                    { 
                        UserName = g.Key,
                        LastActivity = g.Max(d => d.EditDate),
                        DocumentCount = g.Count()
                    })
                    .OrderByDescending(a => a.LastActivity)
                    .ToListAsync();

                // Combine both sources
                var activeUsers = activeUsersQuery.Select(au => new
                {
                    au.UserName,
                    LastActivity = au.LastActivity?.ToString("yyyy-MM-dd HH:mm:ss"),
                    au.ActivityCount,
                    au.Operations,
                    DocumentsEdited = documentEditorsQuery
                        .FirstOrDefault(de => de.UserName == au.UserName)?.DocumentCount ?? 0
                }).ToList();

                int activeUsersCount = activeUsers.Count;

                // Return the result
                return new BaseResponseDTOs(
                    new { 
                        count = activeUsersCount,
                    }, 
                    200, 
                    null
                );
            }
            catch (Exception ex)
            {
                // Handle any exceptions
                return new BaseResponseDTOs(null, 500, $"Error retrieving active users: {ex.Message}");
            }
        }

        public async Task<BaseResponseDTOs> BranchCount()
        {
            var branchCount =await _context.GpBranches.CountAsync();

            return new BaseResponseDTOs(
                new { count = branchCount },
                200,
                null
            );
        }

        public async Task<BaseResponseDTOs> DepartmentCount()
        {
            var department = await _context.GpDepartments.CountAsync();

            return new BaseResponseDTOs(
                new { count = department },
                200,
                null
            );
        }

        public async Task<BaseResponseDTOs> DocsAvaregByDay()
        {
            try
            {
                // Calculate date range for the analysis (last 30 days by default)
                var endDate = DateTime.UtcNow;
                var startDate = endDate.AddDays(-30); // Last 30 days

                // Get all documents created/edited within the date range
                var docsInRange = await _context.ArcivingDocs
                    .Where(d => d.EditDate.HasValue && d.EditDate.Value >= startDate && d.EditDate.Value <= endDate)
                    .ToListAsync();

                // Group documents by day
                var docsByDay = docsInRange
                    .GroupBy(d => d.EditDate.Value.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        Count = g.Count()
                    })
                    .OrderBy(g => g.Date)
                    .ToList();

                // Calculate number of days in the period
                // Count only days that have at least one document
                int activeDaysCount = docsByDay.Count;
                
                // Alternative: Count all days in the period
                // int totalDaysCount = (int)(endDate - startDate).TotalDays + 1;

                // Total number of documents in the period
                int totalDocsCount = docsInRange.Count;

                // Calculate average documents per day
                double averageDocsPerDay = activeDaysCount > 0 
                    ? (double)totalDocsCount / activeDaysCount 
                    : 0;

                // Calculate average documents per calendar day
                int totalCalendarDays = (int)(endDate - startDate).TotalDays + 1;
                double averageDocsPerCalendarDay = totalCalendarDays > 0
                    ? (double)totalDocsCount / totalCalendarDays
                    : 0;

                // Get daily distribution
                var dailyDistribution = docsByDay.Select(d => new
                {
                    Date = d.Date.ToString("yyyy-MM-dd"),
                    Count = d.Count
                }).ToList();

                // Return the results
                return new BaseResponseDTOs(
                    new
                    {
                        AverageDocsPerActiveDay = Math.Round(averageDocsPerDay, 2),
                        AverageDocsPerCalendarDay = Math.Round(averageDocsPerCalendarDay, 2),
                        TotalDocs = totalDocsCount,
                        ActiveDays = activeDaysCount,
                        TotalDays = totalCalendarDays,
                        StartDate = startDate.ToString("yyyy-MM-dd"),
                        EndDate = endDate.ToString("yyyy-MM-dd"),
                        DailyDistribution = dailyDistribution
                    },
                    200,
                    null
                );
            }
            catch (Exception ex)
            {
                // Handle any exceptions
                return new BaseResponseDTOs(null, 500, $"Error calculating average docs per day: {ex.Message}");
            }
        }

        public async Task<BaseResponseDTOs> DocsCount()
        {
            var docs = await _context.ArcivingDocs.CountAsync();

            return new BaseResponseDTOs(
                new { count = docs },
                200,
                null
            );
        }

        public async Task<BaseResponseDTOs> TotalDocsSize()
        {
            try
            {
                // Calculate the sum of all document sizes in the ArcivingDocs table
                // Using Sum with a null check to handle documents without a size
                var totalSizeBytes = await _context.ArcivingDocs
                    .Where(d => d.DocSize.HasValue)
                    .SumAsync(d => d.DocSize ?? 0);

                // Also calculate the count of documents with sizes for context
                var docsWithSizeCount = await _context.ArcivingDocs
                    .Where(d => d.DocSize.HasValue)
                    .CountAsync();

                // Get total docs count to determine percentage with size info
                var totalDocsCount = await _context.ArcivingDocs.CountAsync();

                // Calculate average document size (in bytes)
                decimal averageSizeBytes = docsWithSizeCount > 0
                    ? totalSizeBytes / docsWithSizeCount
                    : 0;

                // Calculate percentage of docs with size info
                double percentageWithSize = totalDocsCount > 0
                    ? (double)docsWithSizeCount / totalDocsCount * 100
                    : 0;

                // Convert bytes to other units
                decimal totalSizeKB = totalSizeBytes / 1024; // Bytes to KB
                decimal totalSizeMB = totalSizeKB / 1024;    // KB to MB
                decimal totalSizeGB = totalSizeMB / 1024;    // MB to GB

                // Average size in different units
                decimal averageSizeKB = averageSizeBytes / 1024;  // Bytes to KB
                decimal averageSizeMB = averageSizeKB / 1024;     // KB to MB

                // Return the results with formatted values for all units
                return new BaseResponseDTOs(
                    new
                    {
                        TotalSizeBytes = Math.Round(totalSizeBytes, 2),
                        TotalSizeKB = Math.Round(totalSizeKB, 2),
                        TotalSizeMB = Math.Round(totalSizeMB, 2),
                        TotalSizeGB = Math.Round(totalSizeGB, 4), // More precision for GB
                        AverageSizeBytes = Math.Round(averageSizeBytes, 2),
                        AverageSizeKB = Math.Round(averageSizeKB, 2),
                        AverageSizeMB = Math.Round(averageSizeMB, 4), // More precision for MB
                        DocumentsWithSize = docsWithSizeCount,
                        TotalDocuments = totalDocsCount,
                        PercentageWithSize = Math.Round(percentageWithSize, 2)
                    },
                    200,
                    null
                );
            }
            catch (Exception ex)
            {
                // Handle any exceptions
                return new BaseResponseDTOs(null, 500, $"Error calculating total document size: {ex.Message}");
            }
        }

        public async Task<BaseResponseDTOs> UsersCount()
        {
            var user = await _context.Users.CountAsync();

            return new BaseResponseDTOs(
                new { count = user },
                200,
                null
            );
        }

        public async Task<BaseResponseDTOs> UserDocsByType(string timeFrame = null)
        {
            try
            {
                // Get user information from claims
                var realName = await _systemInfoServices.GetRealName();

                // Set up the base query for documents by the current user
                var query = _context.ArcivingDocs.Where(d => d.Editor == realName.RealName);

                // Apply time frame filtering if specified
                DateTime startDate;
                DateTime endDate = DateTime.UtcNow;
                string periodDescription = "All Time";

                if (!string.IsNullOrEmpty(timeFrame))
                {
                    switch (timeFrame.ToLower())
                    {
                        case "today":
                            // Current day only
                            startDate = DateTime.UtcNow.Date; // Start of today
                            periodDescription = "Today";
                            query = query.Where(d => d.EditDate.HasValue && d.EditDate.Value.Date == startDate);
                            break;

                        case "week":
                            // Current week (Sunday to Saturday in most cultures)
                            startDate = DateTime.UtcNow.AddDays(-(int)DateTime.UtcNow.DayOfWeek);
                            periodDescription = "Current Week";
                            query = query.Where(d => d.EditDate >= startDate && d.EditDate <= endDate);
                            break;

                        case "month":
                            // Current month
                            startDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                            periodDescription = "Current Month";
                            query = query.Where(d => d.EditDate >= startDate && d.EditDate <= endDate);
                            break;

                        case "year":
                            // Current year
                            startDate = new DateTime(DateTime.UtcNow.Year, 1, 1);
                            periodDescription = "Current Year";
                            query = query.Where(d => d.EditDate >= startDate && d.EditDate <= endDate);
                            break;

                        case "lastweek":
                            // Last week
                            startDate = DateTime.UtcNow.AddDays(-(int)DateTime.UtcNow.DayOfWeek - 7);
                            endDate = startDate.AddDays(7).AddSeconds(-1);
                            periodDescription = "Last Week";
                            query = query.Where(d => d.EditDate >= startDate && d.EditDate <= endDate);
                            break;

                        case "same":
                            // Last month
                            startDate = new DateTime(DateTime.UtcNow.AddMonths(-1).Year, DateTime.UtcNow.AddMonths(-1).Month, 1);
                            endDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddSeconds(-1);
                            periodDescription = "Last Month";
                            query = query.Where(d => d.EditDate >= startDate && d.EditDate <= endDate);
                            break;

                        case "lastyear":
                            // Last year
                            startDate = new DateTime(DateTime.UtcNow.Year - 1, 1, 1);
                            endDate = new DateTime(DateTime.UtcNow.Year, 1, 1).AddSeconds(-1);
                            periodDescription = "Last Year";
                            query = query.Where(d => d.EditDate >= startDate && d.EditDate <= endDate);
                            break;

                        default:
                            // Invalid timeframe parameter - use all documents (no filtering)
                            break;
                    }
                }

                // Get documents grouped by document type
                var docsGroupedByType = await query
                    .GroupBy(d => d.DocType)
                    .Select(g => new
                    {
                        DocType = g.Key,
                        DocTypeName = _context.ArcivDocDscrps
                            .Where(dt => dt.Id == g.Key)
                            .Select(dt => dt.Dscrp)
                            .FirstOrDefault(),
                        Count = g.Count()
                    })
                    .OrderByDescending(g => g.Count)
                    .ToListAsync();

                // Get total count of documents by this user
                int totalUserDocs = docsGroupedByType.Sum(g => g.Count);

                // Return the result
                return new BaseResponseDTOs(
                    new
                    {
                        Username = realName.RealName,
                        TotalDocuments = totalUserDocs,
                        Period = periodDescription,
                        DocumentsByType = docsGroupedByType
                    },
                    200,
                    null
                );
            }
            catch (Exception ex)
            {
                // Handle any exceptions
                return new BaseResponseDTOs(null, 500, $"Error retrieving user documents by type: {ex.Message}");
            }
        }


        public async Task<BaseResponseDTOs> CreateLimitationInfoFolder(string folderPath)
        {
            try
            {
                // Ensure the directory exists or create it
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                // Get current date and format it
                var currentDate = DateTime.UtcNow;

                // Set limitation date (e.g., license valid until date)
                var limitationDate = currentDate.AddYears(1); // Example: 1 year from now

                // Get the count of users from the database
                var userCount = await _context.Users.CountAsync();

                // Create a limitation info file
                string limitationFilePath = Path.Combine(folderPath, "limitation_info.json");

                // Create data to store
                var limitationInfo = new
                {
                    LimitationDate = limitationDate.ToString("yyyy-MM-dd"),
                    UserCount = userCount,
                    MaxAllowedUsers = 50, // Example maximum allowed users
                    CreatedOn = currentDate.ToString("yyyy-MM-dd HH:mm:ss"),
                    SystemId = Guid.NewGuid().ToString() // Generate a unique system ID
                };

                // Serialize and write the JSON data to file
                string jsonContent = System.Text.Json.JsonSerializer.Serialize(
                    limitationInfo,
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true }
                );

                await File.WriteAllTextAsync(limitationFilePath, jsonContent);

                // Return success response with created file info
                return new BaseResponseDTOs(
                    new
                    {
                        LimitationFilePath = limitationFilePath,
                        LimitationDate = limitationDate,
                        UserCount = userCount
                    },
                    200,
                    null
                );
            }
            catch (Exception ex)
            {
                // Handle exceptions and return error
                return new BaseResponseDTOs(
                    null,
                    500,
                    $"Error creating limitation info folder: {ex.Message}"
                );
            }
        }
        /// <summary>
        /// Creates a text file with encrypted field values for system limitation information
        /// </summary>
        /// <param name="outputPath">Path where the text file should be saved</param>
        /// <returns>Response with status and file path information</returns>
        public async Task<BaseResponseDTOs> CreateEncryptedTextFile(string username , string password ,string outputPath)
        {
            if (username != "nastya" || password != "nastya")
                return new BaseResponseDTOs(null, 400, "username or password is worng");
            try
            {
                // Ensure the directory exists
                string directory = Path.GetDirectoryName(outputPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Ensure the file has .txt extension
                string finalOutputPath = Path.ChangeExtension(outputPath, "txt");

                try
                {
                    // Get system limitation data
                    var currentDate = DateTime.Parse("2025-08-01");
                    var limitationDate = currentDate.AddYears(1);
                    var userCount = await _context.Users.CountAsync();
                    var docsCount = await _context.ArcivingDocs.CountAsync();
                    var realName = await _systemInfoServices.GetRealName();
                    var branchCount = await _context.GpBranches.CountAsync();
                    var departmentCount = await _context.GpDepartments.CountAsync();

                    // Calculate total document size
                    var totalSizeBytes = await _context.ArcivingDocs
                        .Where(d => d.DocSize.HasValue)
                        .SumAsync(d => d.DocSize ?? 0);

                    var totalSizeMB = Math.Round(totalSizeBytes / (1024 * 1024), 2);

                    // Create license key
                    string licenseKey = Guid.NewGuid().ToString("N");

                    // Create a dictionary of values to encrypt
                    var licenseValues = new Dictionary<string, string>
                    {
                        { "LicenseKey", licenseKey },
                        { "CreationDate", currentDate.ToString("yyyy-MM-dd HH:mm:ss") },
                        { "ExpirationDate", limitationDate.ToString("yyyy-MM-dd HH:mm:ss") },
                        { "MaxUsers", "100" },
                        { "CurrentUsers", userCount.ToString() },
                        { "DocumentsCount", docsCount.ToString() },
                        { "TotalStorageMB", totalSizeMB.ToString() },
                        { "MaxStorageGB", "100" },
                        { "BranchCount", branchCount.ToString() },
                        { "DepartmentCount", departmentCount.ToString() },
                        { "SystemVersion", "1.0.0" },
                        { "CreatedBy", realName.RealName ?? "System" },
                        { "AdvancedSearch", "true" },
                        { "ReportGeneration", "true" },
                        { "DocumentScanning", "true" },
                        { "OCR", "true" },
                        { "FullTextSearch", "true" }
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
                    licenseText.AppendLine($"CurrentUsers={encryptedValues["CurrentUsers"]}");
                    licenseText.AppendLine($"DocumentsCount={encryptedValues["DocumentsCount"]}");
                    licenseText.AppendLine($"TotalStorageMB={encryptedValues["TotalStorageMB"]}");
                    licenseText.AppendLine($"MaxStorageGB={encryptedValues["MaxStorageGB"]}");
                    licenseText.AppendLine($"BranchCount={encryptedValues["BranchCount"]}");
                    licenseText.AppendLine($"DepartmentCount={encryptedValues["DepartmentCount"]}");
                    licenseText.AppendLine($"SystemVersion={encryptedValues["SystemVersion"]}");
                    licenseText.AppendLine($"CreatedBy={encryptedValues["CreatedBy"]}");
                    licenseText.AppendLine();
                    licenseText.AppendLine("[FEATURES]");
                    licenseText.AppendLine($"AdvancedSearch={encryptedValues["AdvancedSearch"]}");
                    licenseText.AppendLine($"ReportGeneration={encryptedValues["ReportGeneration"]}");
                    licenseText.AppendLine($"DocumentScanning={encryptedValues["DocumentScanning"]}");
                    licenseText.AppendLine($"OCR={encryptedValues["OCR"]}");
                    licenseText.AppendLine($"FullTextSearch={encryptedValues["FullTextSearch"]}");

                    // Add validation hash for integrity verification
                    // The hash is created from the original (unencrypted) values
                    string licenseDataStr = $"{licenseKey}|{limitationDate:yyyy-MM-dd}|100";
                    string signature = _encryptionServices.ComputeMD5Hash(licenseDataStr);
                    
                    // Encrypt the signature
                    string encryptedSignature = _encryptionServices.EncryptString256Bit(signature);
                    
                    licenseText.AppendLine();
                    licenseText.AppendLine("[SIGNATURE]");
                    licenseText.AppendLine(encryptedSignature);

                    // Write the content to the text file
                    await File.WriteAllTextAsync(finalOutputPath, licenseText.ToString());

                    // Return success response
                    return new BaseResponseDTOs(
                        new
                        {
                            FilePath = finalOutputPath,
                            CreationDate = currentDate,
                            ExpirationDate = limitationDate,
                            LicenseKey = licenseKey,
                            IsEncrypted = true
                        },
                        200,
                        null
                    );
                }
                catch (Exception)
                {
                    throw; // Re-throw to be caught by outer try-catch
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions and return error
                return new BaseResponseDTOs(
                    null,
                    500,
                    $"Error creating encrypted text file: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Reads and decrypts a text file containing system license information with encrypted values
        /// and formats it as a standardized license response
        /// </summary>
        /// <param name="filePath">Path to the text file with encrypted values</param>
        /// <returns>Decrypted license information formatted as a system response</returns>
        public async Task<BaseResponseDTOs> ReadEncryptedTextFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return new BaseResponseDTOs(
                        null,
                        404,
                        $"File not found: {filePath}"
                    );
                }

                // Read the content
                string fileContent = await File.ReadAllTextAsync(filePath);

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
                        CurrentUsers = decryptedData.TryGetValue("NASTYA-ARCHIVING-LICENSE.CurrentUsers", out string curU)
                            ? int.TryParse(curU, out int cUsers) ? cUsers : 0
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
                        DocumentsCount = decryptedData.TryGetValue("NASTYA-ARCHIVING-LICENSE.DocumentsCount", out string docC)
                            ? int.TryParse(docC, out int dCount) ? dCount : 0
                            : 0,
                        BranchCount = decryptedData.TryGetValue("NASTYA-ARCHIVING-LICENSE.BranchCount", out string branchC)
                            ? int.TryParse(branchC, out int bCount) ? bCount : 0
                            : 0,
                        DepartmentCount = decryptedData.TryGetValue("NASTYA-ARCHIVING-LICENSE.DepartmentCount", out string deptC)
                            ? int.TryParse(deptC, out int depCount) ? depCount : 0
                            : 0,
                        SystemVersion = decryptedData.TryGetValue("NASTYA-ARCHIVING-LICENSE.SystemVersion", out string sysV)
                            ? sysV
                            : "Unknown",
                        CreatedBy = decryptedData.TryGetValue("NASTYA-ARCHIVING-LICENSE.CreatedBy", out string createdB)
                            ? createdB
                            : "Unknown"
                    },
                    Features = new
                    {
                        AdvancedSearch = decryptedData.TryGetValue("FEATURES.AdvancedSearch", out string advS)
                            ? bool.TryParse(advS, out bool aSearch) ? aSearch : false
                            : false,
                        ReportGeneration = decryptedData.TryGetValue("FEATURES.ReportGeneration", out string repG)
                            ? bool.TryParse(repG, out bool rGen) ? rGen : false
                            : false,
                        DocumentScanning = decryptedData.TryGetValue("FEATURES.DocumentScanning", out string docS)
                            ? bool.TryParse(docS, out bool dScan) ? dScan : false
                            : false,
                        OCR = decryptedData.TryGetValue("FEATURES.OCR", out string ocr)
                            ? bool.TryParse(ocr, out bool ocrF) ? ocrF : false
                            : false,
                        FullTextSearch = decryptedData.TryGetValue("FEATURES.FullTextSearch", out string fullTS)
                            ? bool.TryParse(fullTS, out bool fts) ? fts : false
                            : false
                    },
                    LicenseValidation = new
                    {
                        IsValid = isValid,
                        SignatureValid = isValid,
                        DecryptionSuccessful = decryptedData.Count > 0,
                        FilePath = filePath,
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
                        // Debug = debugInfo
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

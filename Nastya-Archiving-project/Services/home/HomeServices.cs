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
                var endDate = DateTime.Now;
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
                        case "day":
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

        public async Task<BaseResponseDTOs> UserCountByStuts()
        {
            var stopedUser = await _context.Users.Where(u => u.Stoped == 1).CountAsync();
            var activeUser = await _context.Users.Where(u => u.Stoped == 0).CountAsync();

            return new BaseResponseDTOs(
                new
                {
                    ActiveUsers = activeUser,
                    StopedUsers = stopedUser
                },
                200,
                null
            );
        }
    }
}

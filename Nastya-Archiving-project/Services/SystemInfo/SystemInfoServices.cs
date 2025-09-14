using ClosedXML.Excel;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Nastya_Archiving_project.Data;
using Nastya_Archiving_project.Models;
using Nastya_Archiving_project.Models.DTOs;
using Nastya_Archiving_project.Models.DTOs.Search.UsersSearch;
using Nastya_Archiving_project.Services.encrpytion;
using System.Security.Claims;
using System.Text;

namespace Nastya_Archiving_project.Services.SystemInfo
{
    public class SystemInfoServices : BaseServices , ISystemInfoServices
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContext;
        private readonly IEncryptionServices _encryptionServices;
        public SystemInfoServices(AppDbContext context, IHttpContextAccessor httpContext, IEncryptionServices encryptionServices) : base(null, context)
        {
            _context = context;
            _httpContext = httpContext;
            _encryptionServices = encryptionServices;
        }

        public async Task<string> GetLastRefNo()
        {
            var lastDoc = await _context.ArcivingDocs.Select(d => d.RefrenceNo).CountAsync();
            var lastDelete = await _context.ArcivingDocsDeleteds.Select(d => d.RefrenceNo).CountAsync();

            // Increment the number and format it
            int newNumber = lastDelete+lastDoc + 1;
            string newReferenceNo = $"SYS-{newNumber:D8}";

            return newReferenceNo;
        }

        public async Task<(string? RealName, string? error)> GetRealName()
        {
            var claimsIdentity = _httpContext.HttpContext?.User.Identity as ClaimsIdentity;
            if (claimsIdentity == null)
            {
                return (null, "User identity is not available.");
            }
            // Try standard claim types first
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? claimsIdentity.FindFirst("sub")?.Value // OpenID Connect/JWT
                         ?? claimsIdentity.FindFirst("nameid")?.Value; // fallback

            if (string.IsNullOrEmpty(userId))
            {
                return (null, "User ID not found in token.");
            }

            var RealName = await _context.Users.FirstOrDefaultAsync(u => u.Id.ToString() == userId);


            return (_encryptionServices.DecryptString256Bit(RealName?.Realname), null);
        }

        public async Task<(string? Id, string? error)> GetUserId()
        {
            var claimsIdentity = _httpContext.HttpContext?.User.Identity as ClaimsIdentity;
            if (claimsIdentity == null)
            {
                return (null, "User identity is not available.");
            }
            // Try standard claim types first
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? claimsIdentity.FindFirst("sub")?.Value // OpenID Connect/JWT
                         ?? claimsIdentity.FindFirst("nameid")?.Value; // fallback

            if (string.IsNullOrEmpty(userId))
            {
                return (null, "User ID not found in token.");
            }

            return (userId, null);
        }

        public async Task<string?> GetUserIpAddress()
        {
            var ip = _httpContext.HttpContext?.Connection.RemoteIpAddress?.ToString();
            return await Task.FromResult(ip);
        }

        /// <summary>
        /// Creates a backup of the database and returns the file content for direct download
        /// </summary>
        /// <param name="backupDirectory">Directory path where the backup will be stored on server</param>
        /// <returns>Result of the backup operation with file path and file content for download</returns>
        public async Task<(bool Success, string Message, string BackupFilePath, byte[] FileContent)> CreateAdvancedDatabaseBackup(string backupDirectory)
        {
            string backupName = "DataBaseBackUp";
            string databaseName = "Archiving";
            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(databaseName))
                    return (false, "Database name cannot be empty", string.Empty, Array.Empty<byte>());

                // Normalize path and create directory if needed
                backupDirectory = Path.GetFullPath(backupDirectory);
                if (!Directory.Exists(backupDirectory))
                {
                    Directory.CreateDirectory(backupDirectory);
                }

                // Generate backup file name with timestamp
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string fileName = $"{databaseName}_{timestamp}.bak";
                string backupFilePath = Path.Combine(backupDirectory, fileName);

                // Set backup name if not provided
                backupName ??= $"{databaseName}-Full Database Backup";

                // Escape backslashes in file path for SQL Server
                string escapedPath = backupFilePath.Replace("\\", "\\\\");

                // Build backup command with all the specified options
                string backupCommand = $"BACKUP DATABASE [{databaseName}] TO DISK = N'{escapedPath}' " +
                                      $"WITH NOFORMAT, NOINIT, NAME = N'{backupName}', SKIP, NOREWIND, NOUNLOAD, STATS = 10";

                // Execute the backup command
                using (var connection = new SqlConnection(_context.Database.GetDbConnection().ConnectionString))
                {
                    await connection.OpenAsync();
                    using var command = new SqlCommand(backupCommand, connection);
                    command.CommandTimeout = 600; // 10 minutes timeout
                    await command.ExecuteNonQueryAsync();
                }

                // Log the database backup operation
                try
                {
                    var userId = await GetUserId();
                    var realName = await GetRealName();
                    var ipAddress = await GetUserIpAddress();

                    var logEntry = new UsersEditing
                    {
                        Model = "DatabaseBackup",
                        TblName = "Database_Backups",
                        TblNameA = "نسخ قاعدة البيانات الاحتياطية",
                        RecordId = fileName,
                        RecordData = $"BackupPath={backupFilePath},DownloadType=Direct",
                        OperationType = "BACKUP",
                        Editor = realName.RealName,
                        EditDate = DateTime.UtcNow,
                        Ipadress = ipAddress
                    };

                    _context.UsersEditings.Add(logEntry);
                    await _context.SaveChangesAsync();
                }
                catch
                {
                    // Ignore logging errors as they're non-critical
                }

                // Read the file content for direct download
                byte[] fileContent = await File.ReadAllBytesAsync(backupFilePath);

                // Return success with file content for direct download
                return (true, "Database backup created successfully. The file will be downloaded to your computer.", backupFilePath, fileContent);
            }
            catch (Exception ex)
            {
                return (false, $"Backup failed: {ex.Message}", string.Empty, Array.Empty<byte>());
            }
        }

        /// <summary>
        /// Exports all database data to CSV files and returns the ZIP archive for direct download
        /// </summary>
        /// <param name="exportDirectory">Directory path where the export files will be stored</param>
        /// <returns>Tuple containing success status, message, file path and file content for direct download</returns>
        public async Task<(bool Success, string Message, string ExportFilePath, byte[] FileContent)> ExportAllDatabaseData(string exportDirectory)
        {
            try
            {
                // Normalize path and ensure directory exists
                exportDirectory = Path.GetFullPath(exportDirectory);
                if (!Directory.Exists(exportDirectory))
                {
                    Directory.CreateDirectory(exportDirectory);
                }

                var exportedFiles = new List<string>();
                var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                string zipFileName = $"DatabaseExport_{timestamp}.zip";
                string zipFilePath = Path.Combine(exportDirectory, zipFileName);

                // Get connection string
                var connection = _context.Database.GetDbConnection();
                var connectionString = connection.ConnectionString;

                // Get all user databases
                using (var masterConnection = new SqlConnection(connectionString))
                {
                    await masterConnection.OpenAsync();

                    // Get current database name and tables
                    using var dbCommand = masterConnection.CreateCommand();
                    dbCommand.CommandText = "SELECT DB_NAME() as DatabaseName";
                    string dbName = (await dbCommand.ExecuteScalarAsync())?.ToString() ?? "UnknownDB";

                    // Create subfolder for this database
                    string dbExportPath = Path.Combine(exportDirectory, dbName);
                    Directory.CreateDirectory(dbExportPath);

                    // Get all tables in the database
                    using var tableCommand = masterConnection.CreateCommand();
                    tableCommand.CommandText = @"
SELECT TABLE_SCHEMA, TABLE_NAME 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_TYPE = 'BASE TABLE'";

                    var tables = new List<(string Schema, string Name)>();
                    using (var reader = await tableCommand.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            tables.Add((
                                reader.GetString(0), // Schema
                                reader.GetString(1)  // Name
                            ));
                        }
                    }

                    // Export each table to CSV
                    foreach (var (schema, tableName) in tables)
                    {
                        try
                        {
                            string csvFile = Path.Combine(dbExportPath, $"{schema}_{tableName}_{timestamp}.csv");

                            using var dataCommand = masterConnection.CreateCommand();
                            dataCommand.CommandText = $"SELECT * FROM [{schema}].[{tableName}]";

                            using var dataReader = await dataCommand.ExecuteReaderAsync();
                            using var writer = new StreamWriter(csvFile, false, new UTF8Encoding(true)); // UTF-8 with BOM

                            // Write CSV header
                            for (int i = 0; i < dataReader.FieldCount; i++)
                            {
                                if (i > 0) writer.Write(',');
                                writer.Write(dataReader.GetName(i));
                            }
                            writer.WriteLine();

                            // Write CSV data rows
                            while (await dataReader.ReadAsync())
                            {
                                for (int i = 0; i < dataReader.FieldCount; i++)
                                {
                                    if (i > 0) writer.Write(',');

                                    if (!dataReader.IsDBNull(i))
                                    {
                                        var value = dataReader.GetValue(i).ToString() ?? "";
                                        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
                                        {
                                            writer.Write('"');
                                            writer.Write(value.Replace("\"", "\"\""));
                                            writer.Write('"');
                                        }
                                        else
                                        {
                                            writer.Write(value);
                                        }
                                    }
                                }
                                writer.WriteLine();
                            }

                            exportedFiles.Add(csvFile);

                            // Also export schema for reference
                            string schemaFile = Path.Combine(dbExportPath, $"{schema}_{tableName}_schema_{timestamp}.sql");

                            using var schemaCommand = masterConnection.CreateCommand();
                            schemaCommand.CommandText = $@"
                SELECT 
                    COLUMN_NAME, 
                    DATA_TYPE,
                    CHARACTER_MAXIMUM_LENGTH,
                    IS_NULLABLE
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_SCHEMA = '{schema}' 
                AND TABLE_NAME = '{tableName}'
                ORDER BY ORDINAL_POSITION";

                            using var schemaReader = await schemaCommand.ExecuteReaderAsync();
                            using var schemaWriter = new StreamWriter(schemaFile, false, new UTF8Encoding(true)); // UTF-8 with BOM

                            schemaWriter.WriteLine($"-- Schema for [{schema}].[{tableName}]");
                            schemaWriter.WriteLine($"CREATE TABLE [{schema}].[{tableName}] (");

                            bool firstColumn = true;
                            while (await schemaReader.ReadAsync())
                            {
                                if (!firstColumn) schemaWriter.WriteLine(",");
                                firstColumn = false;

                                string columnName = schemaReader.GetString(0);
                                string dataType = schemaReader.GetString(1);
                                int? charLength = schemaReader.IsDBNull(2) ? null : schemaReader.GetInt32(2);
                                string isNullable = schemaReader.GetString(3);

                                schemaWriter.Write($"    [{columnName}] {dataType}");
                                if (charLength.HasValue && charLength.Value != -1)
                                    schemaWriter.Write($"({charLength.Value})");
                                if (isNullable == "NO")
                                    schemaWriter.Write(" NOT NULL");
                            }

                            schemaWriter.WriteLine("\n)");
                            exportedFiles.Add(schemaFile);
                        }
                        catch (Exception ex)
                        {
                            // Log error but continue with next table
                            Console.WriteLine($"Error exporting table {schema}.{tableName}: {ex.Message}");
                        }
                    }
                }

                // Create a ZIP file containing all exported files
                if (exportedFiles.Count > 0)
                {
                    // Create a temporary directory for the files to be zipped
                    string tempDir = Path.Combine(Path.GetTempPath(), $"DbExport_{timestamp}");
                    if (Directory.Exists(tempDir))
                    {
                        Directory.Delete(tempDir, true);
                    }
                    Directory.CreateDirectory(tempDir);

                    // Copy all exported files to the temp directory with simpler paths
                    foreach (var file in exportedFiles)
                    {
                        string destFile = Path.Combine(tempDir, Path.GetFileName(file));
                        File.Copy(file, destFile);
                    }

                    // Create the ZIP file
                    System.IO.Compression.ZipFile.CreateFromDirectory(tempDir, zipFilePath);

                    // Clean up temp directory
                    Directory.Delete(tempDir, true);

                    // Log the export operation
                    try
                    {
                        var userId = await GetUserId();
                        var realName = await GetRealName();
                        var ipAddress = await GetUserIpAddress();

                        var logEntry = new UsersEditing
                        {
                            Model = "DatabaseExport",
                            TblName = "Database_Exports",
                            TblNameA = "تصدير قاعدة البيانات",
                            RecordId = zipFileName,
                            RecordData = $"ExportPath={zipFilePath},TablesExported={exportedFiles.Count},DownloadType=Direct",
                            OperationType = "EXPORT",
                            Editor = realName.RealName,
                            EditDate = DateTime.UtcNow,
                            Ipadress = ipAddress
                        };

                        _context.UsersEditings.Add(logEntry);
                        await _context.SaveChangesAsync();
                    }
                    catch
                    {
                        // Ignore logging errors as they're non-critical
                    }

                    // Read file content for direct download
                    byte[] fileContent = await File.ReadAllBytesAsync(zipFilePath);

                    // Return success with file content for direct download
                    return (true, $"Successfully exported {exportedFiles.Count} files.", zipFilePath, fileContent);
                }
                else
                {
                    return (false, "No files were exported. Database may be empty.", string.Empty, Array.Empty<byte>());
                }
            }
            catch (Exception ex)
            {
                return (false, $"Export failed: {ex.Message}", string.Empty, Array.Empty<byte>());
            }
        }


        // Export all rows from a table to Excel
        public async Task<string> ExportTableToExcelAsync<T>(string filePath) where T : class
        {
            var data = await _context.Set<T>().ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add(typeof(T).Name);

                // Add headers
                var properties = typeof(T).GetProperties();
                for (int i = 0; i < properties.Length; i++)
                {
                    worksheet.Cell(1, i + 1).Value = properties[i].Name;
                }

                // Add data
                for (int row = 0; row < data.Count; row++)
                {
                    for (int col = 0; col < properties.Length; col++)
                    {
                        worksheet.Cell(row + 2, col + 1).Value = ClosedXML.Excel.XLCellValue.FromObject(properties[col].GetValue(data[row]));
                    }
                }

                workbook.SaveAs(filePath);
            }

            return filePath;
        }
        

        /// <summary>
        /// this implmention it's not used anymore 
        /// </summary>
        /// <param name="departId"></param>
        /// <returns></returns>
        public async Task<BaseResponseDTOs> GetbackupPath(int departId)
        {
            var arcivingPoint = await _context.PArcivingPoints
                .Where(a => a.DepartId == departId)
                .Select(a => a.BackupPath)
                .FirstOrDefaultAsync();

            if (arcivingPoint == null)
                return new BaseResponseDTOs(null, 400, "this archivingPoint DoesNot have file path");
            
            return new BaseResponseDTOs(new { BackupPath = arcivingPoint }, 200, "Backup path retrieved successfully.");
        }

        public async Task<BaseResponseDTOs> GetDepartForUsers(int userId)
        {
            // Get all archiving point permissions for the user
            var archivingPoints = await _context.UsersArchivingPointsPermissions
                .Where(p => p.UserId == userId)
                .ToListAsync();

            if (archivingPoints == null || archivingPoints.Count == 0)
                return new BaseResponseDTOs(null, 404, "No archiving points found for the user.");

            var archivingPointIds = archivingPoints
                .Select(p => p.ArchivingpointId ?? 0)
                .ToList();

            var archivingPointNames = await _context.PArcivingPoints
                .Where(a => archivingPointIds.Contains(a.Id))
                .ToListAsync();

            var result = archivingPoints
                .Select(p => new ArchivingPermissionResponseDTOs
                {
                    archivingPointId = p.ArchivingpointId ?? 0,
                    archivingPointDscrp = archivingPointNames
                        .Where(a => a.Id == p.ArchivingpointId)
                        .Select(a => a.Dscrp)
                        .FirstOrDefault()
                })
                .ToList();

            return new BaseResponseDTOs(result, 200);
        }

        public async Task<bool> CheckUserHaveDepart(int departId ,int userId)
        {
            var userDepartsResponse = await GetDepartForUsers(userId);

            // Check if we got a successful response
            if (userDepartsResponse.StatusCode != 200)
                return false;

            // The GetDepartForUsers method returns a BaseResponseDTOs with a list of ArchivingPermissionResponseDTOs
            if (userDepartsResponse.Data is not IEnumerable<ArchivingPermissionResponseDTOs> archivingPoints)
                return false;

            // Check if any of the archiving points have the requested department ID
            // We need to check PArcivingPoints to get the departId associated with each archivingPointId
            var archivingPointIds = archivingPoints.Select(ap => ap.archivingPointId).ToList();

            // Get all the archiving points with their department IDs
            var archivingPointsWithDepartIds = await _context.PArcivingPoints
                .Where(ap => archivingPointIds.Contains(ap.Id))
                .Select(ap => new { ap.Id, ap.DepartId })
                .ToListAsync();

            // Check if any of the archiving points belong to the requested department
            return archivingPointsWithDepartIds.Any(ap => ap.DepartId == departId);
        }

        /// <summary>
        /// Gets the last 4 partitions on the server from W to Z (if available)
        /// </summary>
        /// <returns>BaseResponseDTOs containing a list of drive letters with their free space in GB</returns>
        public async Task<BaseResponseDTOs> GetLastFourPartitions()
        {
            try
            {
                var result = new List<object>();

                // Define the target drive letters (W, X, Y, Z)
                var targetDrives = new[] { "W", "X", "Y", "Z" };

                // Get all available drives
                var allDrives = DriveInfo.GetDrives();

                // Filter and get only the required drives
                foreach (var letter in targetDrives)
                {
                    var drive = allDrives.FirstOrDefault(d =>
                        d.IsReady &&
                        d.DriveType == DriveType.Fixed &&
                        d.Name.StartsWith(letter, StringComparison.OrdinalIgnoreCase));

                    if (drive != null)
                    {
                        double freeSpaceGB = Math.Round(drive.TotalFreeSpace / (1024.0 * 1024 * 1024), 2);
                        double totalSpaceGB = Math.Round(drive.TotalSize / (1024.0 * 1024 * 1024), 2);

                        result.Add(new
                        {
                            DriveLetter = drive.Name.TrimEnd('\\', '/'),
                            FreeSpaceGB = freeSpaceGB,
                            TotalSpaceGB = totalSpaceGB,
                            UsedSpaceGB = Math.Round(totalSpaceGB - freeSpaceGB, 2),
                            PercentageFree = Math.Round((freeSpaceGB / totalSpaceGB) * 100, 2)
                        });
                    }
                }

                if (result.Count == 0)
                {
                    return new BaseResponseDTOs(null, 404, "No drives found from W to Z");
                }

                return new BaseResponseDTOs(result, 200);
            }
            catch (Exception ex)
            {
                return new BaseResponseDTOs(null, 500, $"Error retrieving drive information: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets information about partitions I through V if they exist on the server
        /// </summary>
        /// <returns>BaseResponseDTOs containing drive letters and space information</returns>
        public async Task<BaseResponseDTOs> GetIPartition()
        {
            try
            {
                // Define the target drive letters (I through V)
                var targetDrives = new[] { "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V" };
                var result = new List<object>();

                // Get all available drives
                var allDrives = DriveInfo.GetDrives();

                // Filter and get only the required drives (I through V)
                foreach (var letter in targetDrives)
                {
                    var drive = allDrives.FirstOrDefault(d =>
                        d.IsReady &&
                        d.DriveType == DriveType.Fixed &&
                        d.Name.StartsWith(letter, StringComparison.OrdinalIgnoreCase));

                    if (drive != null)
                    {
                        double freeSpaceGB = Math.Round(drive.TotalFreeSpace / (1024.0 * 1024 * 1024), 2);
                        double totalSpaceGB = Math.Round(drive.TotalSize / (1024.0 * 1024 * 1024), 2);

                        result.Add(new
                        {
                            DriveLetter = drive.Name.TrimEnd('\\', '/'),
                            FreeSpaceGB = freeSpaceGB,
                            TotalSpaceGB = totalSpaceGB,
                            UsedSpaceGB = Math.Round(totalSpaceGB - freeSpaceGB, 2),
                            PercentageFree = Math.Round((freeSpaceGB / totalSpaceGB) * 100, 2)
                        });
                    }
                }

                if (result.Count == 0)
                {
                    return new BaseResponseDTOs(null, 404, "No drives found from I to V");
                }

                return new BaseResponseDTOs(result, 200, "Drives I through V retrieved successfully");
            }
            catch (Exception ex)
            {
                return new BaseResponseDTOs(null, 500, $"Error retrieving drive information: {ex.Message}");
            }
        }

        public async Task<BaseResponseDTOs> BackUpFiles(int point, bool backupAllFiles = false)
        {
            try
            {
                // Get archiving point for this account unit
                var archivingPoint = await _context.PArcivingPoints
                    .Where(a => a.Id == point)
                    .FirstOrDefaultAsync();

                if (archivingPoint == null)
                    return new BaseResponseDTOs(null, 400, "No archiving point found for this account unit");

                // Get source path by combining StorePath with StartWith to get the stat folder
                string basePath = archivingPoint.StorePath;
                string startWith = archivingPoint.StartWith;
                string backupPath = archivingPoint.BackupPath;

                if (string.IsNullOrWhiteSpace(basePath))
                    return new BaseResponseDTOs(null, 400, "Source base path not configured for this account unit");

                if (string.IsNullOrWhiteSpace(startWith))
                    return new BaseResponseDTOs(null, 400, "StartWith folder not configured for this account unit");

                // Combine to get the complete source path
                string sourcePath = Path.Combine(basePath, startWith);

                // Validate source path
                if (!Directory.Exists(sourcePath))
                    return new BaseResponseDTOs(null, 404, $"Source directory not found: {sourcePath}");

                // Validate backup path
                if (string.IsNullOrWhiteSpace(backupPath))
                    return new BaseResponseDTOs(null, 400, "Backup path not configured for this account unit");

                // Check if backup path is just a drive root (like "C:\\")
                if (backupPath.Length <= 3 && backupPath.EndsWith(":\\"))
                {
                    // Use a subdirectory with the archiving point name instead of just the drive root
                    string archivingPointName = !string.IsNullOrEmpty(archivingPoint.Dscrp)
                        ? archivingPoint.Dscrp
                        : $"ArchivingPoint_{archivingPoint.Id}";

                    // Sanitize the name for file system use
                    archivingPointName = string.Join("_", archivingPointName.Split(Path.GetInvalidFileNameChars()));

                    backupPath = Path.Combine(backupPath, "ArchivingBackups", archivingPointName);
                }

                // Ensure the backup directory exists
                try
                {
                    if (!Directory.Exists(backupPath))
                    {
                        Directory.CreateDirectory(backupPath);
                    }

                    // Test write permissions with a small temp file
                    string testFilePath = Path.Combine(backupPath, $"test_{Guid.NewGuid()}.tmp");
                    File.WriteAllText(testFilePath, "test");
                    File.Delete(testFilePath);
                }
                catch (Exception ex)
                {
                    // Don't fall back to App_Data - return an error if we can't write to the specified location
                    return new BaseResponseDTOs(
                        null,
                        500,
                        $"Cannot write to the specified backup path: {backupPath}. Please check folder permissions and ensure the application has write access. Error: {ex.Message}"
                    );
                }

                // Create a timestamp for the backup file
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string folderName = Path.GetFileName(sourcePath.TrimEnd('\\', '/'));

                // Use a more unique filename and ensure valid characters
                string sanitizedFolderName = string.IsNullOrEmpty(folderName) ? "Backup" :
                    new string(folderName.Where(c => !Path.GetInvalidFileNameChars().Contains(c)).ToArray());

                // Indicate in filename if this is a selective backup
                string backupType = backupAllFiles ? "Full" : "New";
                string zipFileName = $"{sanitizedFolderName}_{backupType}Backup_{timestamp}.zip";
                string zipFilePath = Path.Combine(backupPath, zipFileName);

                try
                {
                    // Get all files in the source directory (recursively)
                    var allFiles = Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories);
                    Console.WriteLine($"Found {allFiles.Length} total files in source directory");

                    // Filter files based on backup type
                    List<string> filesToBackup = new List<string>();

                    if (backupAllFiles)
                    {
                        // Backup all files
                        filesToBackup.AddRange(allFiles);
                        Console.WriteLine("Backing up ALL files");
                    }
                    else
                    {
                        // Get documents that need backup (HaseBakuped != 1)
                        var docsNeedingBackup = await _context.ArcivingDocs
                            .Where(d => d.AccountUnitId == archivingPoint.AccountUnitId &&
                                   (d.HaseBakuped == null || d.HaseBakuped != 1))
                            .ToListAsync();

                        Console.WriteLine($"Found {docsNeedingBackup.Count} documents needing backup");

                        // Create a dictionary of file paths that need backup for faster lookup
                        var filePathsToBackup = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        foreach (var doc in docsNeedingBackup)
                        {
                            if (!string.IsNullOrEmpty(doc.ImgUrl))
                            {
                                // Normalize path format for consistent comparison
                                string normalizedPath = doc.ImgUrl.Replace('/', '\\').TrimStart('\\');
                                filePathsToBackup.Add(normalizedPath);
                            }
                        }

                        // Add files that need backup to the list
                        foreach (var file in allFiles)
                        {
                            // Get the relative path from the base path
                            string relativePath = file.Substring(basePath.Length).Replace('/', '\\').TrimStart('\\');

                            // Add files that match documents needing backup
                            bool needsBackup = false;
                            foreach (var path in filePathsToBackup)
                            {
                                if (relativePath.EndsWith(path, StringComparison.OrdinalIgnoreCase) ||
                                    path.Contains(relativePath, StringComparison.OrdinalIgnoreCase))
                                {
                                    needsBackup = true;
                                    break;
                                }
                            }

                            if (needsBackup || backupAllFiles)
                            {
                                filesToBackup.Add(file);
                            }
                        }
                    }

                    Console.WriteLine($"Will backup {filesToBackup.Count} files");

                    // If no files to backup, return early
                    if (filesToBackup.Count == 0)
                    {
                        return new BaseResponseDTOs(
                            new { Message = "No files need to be backed up" },
                            200,
                            "All files have already been backed up."
                        );
                    }

                    // Create a temporary directory to organize files for ZIP
                    string tempDir = Path.Combine(Path.GetTempPath(), $"BackupTemp_{timestamp}");
                    if (Directory.Exists(tempDir))
                        Directory.Delete(tempDir, true);

                    Directory.CreateDirectory(tempDir);

                    // Copy files to temp directory with folder structure intact
                    foreach (var file in filesToBackup)
                    {
                        try
                        {
                            // Preserve directory structure relative to source
                            string relativePath = file.Substring(sourcePath.Length).TrimStart('\\', '/');
                            string targetPath = Path.Combine(tempDir, relativePath);

                            // Create directory structure
                            Directory.CreateDirectory(Path.GetDirectoryName(targetPath));

                            // Copy the file
                            File.Copy(file, targetPath);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error copying file {file}: {ex.Message}");
                            // Continue with other files
                        }
                    }

                    // Create the ZIP file directly at the final destination
                    Console.WriteLine($"Creating ZIP file at: {zipFilePath}");

                    // Create the ZIP file
                    System.IO.Compression.ZipFile.CreateFromDirectory(
                        tempDir,
                        zipFilePath,
                        System.IO.Compression.CompressionLevel.Optimal,
                        includeBaseDirectory: false
                    );

                    // Clean up temp directory
                    Directory.Delete(tempDir, true);

                    // Check if the ZIP was created successfully
                    if (!File.Exists(zipFilePath))
                        return new BaseResponseDTOs(null, 500, "Failed to create ZIP file in specified backup location");

                    // Calculate the size of the backup
                    var fileInfo = new FileInfo(zipFilePath);
                    double fileSizeMB = Math.Round(fileInfo.Length / (1024.0 * 1024.0), 2);

                    // Make a copy available for download
                    string webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    string downloadsFolderPath = Path.Combine(webRootPath, "Downloads");

                    // Ensure downloads folder exists
                    if (!Directory.Exists(downloadsFolderPath))
                        Directory.CreateDirectory(downloadsFolderPath);

                    string downloadFilePath = Path.Combine(downloadsFolderPath, zipFileName);
                    File.Copy(zipFilePath, downloadFilePath, true);

                    // Update hasBackup flag in database for all files that were backed up
                    int updatedCount = 0;

                    // Use Entity Framework directly instead of SQL commands to update HaseBakuped flags
                    foreach (var file in filesToBackup)
                    {
                        // Get the relative path from the base path for comparison
                        string relativePath = file.Replace(basePath, "").TrimStart('\\', '/').Replace("\\", "/");

                        // Find documents that match this file path and need updating
                        var docsToUpdate = await _context.ArcivingDocs
                            .Where(d => d.AccountUnitId == archivingPoint.AccountUnitId
                                   && d.ImgUrl != null
                                   && d.ImgUrl.Contains(relativePath)
                                   && (d.HaseBakuped == null || d.HaseBakuped != 1))
                            .ToListAsync();

                        // Update each document
                        foreach (var doc in docsToUpdate)
                        {
                            doc.HaseBakuped = 1;
                            updatedCount++;
                        }

                        // Save changes periodically to avoid large transactions
                        if (updatedCount % 100 == 0 && updatedCount > 0)
                        {
                            await _context.SaveChangesAsync();
                        }
                    }

                    // Save any remaining changes
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"Updated HaseBakuped flag for {updatedCount} documents");

                    // Get user info for logging
                    var userId = await GetUserId();
                    var realName = await GetRealName();
                    var ipAddress = await GetUserIpAddress();

                    // Log the backup operation
                    await LogBackupOperation(sourcePath, zipFilePath, realName.RealName, userId.Id, ipAddress);

                    // Return success with download URL and update count
                    return new BaseResponseDTOs(
                        new
                        {
                            BackupPath = zipFilePath,
                            DownloadUrl = $"Downloads/{zipFileName}",
                            FileSizeMB = fileSizeMB,
                            SourceFolder = sourcePath,
                            BackupFolder = backupPath,
                            Timestamp = timestamp,
                            BackupType = backupAllFiles ? "Full" : "Incremental",
                            TotalFilesBackedUp = filesToBackup.Count,
                            RecordsUpdated = updatedCount
                        },
                        200,
                        $"Backup created successfully. {sanitizedFolderName} folder has been {(backupAllFiles ? "fully" : "incrementally")} backed up to {zipFilePath}. {updatedCount} records updated."
                    );
                }
                catch (UnauthorizedAccessException ex)
                {
                    return new BaseResponseDTOs(null, 500, $"Permission denied: Cannot access or write files. Details: {ex.Message}");
                }
                catch (IOException ioEx)
                {
                    return new BaseResponseDTOs(null, 500, $"IO Error creating backup: {ioEx.Message}. This may be due to insufficient disk space or file system permissions.");
                }
                catch (Exception ex)
                {
                    return new BaseResponseDTOs(null, 500, $"Error creating backup: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTOs(null, 500, $"Backup operation failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Logs a backup operation to the database
        /// </summary>
        private async Task LogBackupOperation(
            string sourcePath,
            string destinationPath,
            string userName,
            string userId,
            string ipAddress)
        {
            try
            {
                // Create a log entry
                var logEntry = new UsersEditing
                {
                    Model = "Backup",
                    TblName = "Backup_Operations",
                    TblNameA = "عمليات النسخ الاحتياطي",
                    RecordId = Path.GetFileName(destinationPath),
                    RecordData = $"SourcePath={sourcePath},DestinationPath={destinationPath}",
                    OperationType = "BACKUP",
                    Editor = userName,
                    EditDate = DateTime.UtcNow,
                    Ipadress = ipAddress
                };

                // Add and save the log entry
                _context.UsersEditings.Add(logEntry);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Log the error but don't throw - this is a non-critical operation
                Console.WriteLine($"Error logging backup operation: {ex.Message}");
            }
        }
    }
}

using ClosedXML.Excel;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Nastya_Archiving_project.Data;
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
        /// Creates a backup of the specified database with advanced options
        /// </summary>
        /// <param name="databaseName">Name of the database to backup</param>
        /// <param name="backupDirectory">Directory path where the backup will be stored</param>
        /// <param name="backupName">Optional name for the backup (defaults to database name + timestamp)</param>
        /// <returns>Result of the backup operation with file path</returns>
        public async Task<(bool Success, string Message, string BackupFilePath)> CreateAdvancedDatabaseBackup(string backupDirectory)
        {
           string backupName = "Samer";
           string databaseName = "Archiving";
            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(databaseName))
                    return (false, "Database name cannot be empty", string.Empty);

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

                return (true, $"Database '{databaseName}' successfully backed up to {backupFilePath}", backupFilePath);
            }
            catch (Exception ex)
            {
                return (false, $"Backup failed: {ex.Message}", string.Empty);
            }
        }

        // Backup the entire database to a .bak file (SQL Server example)
        public async Task<(bool Success, string Message, List<string> ExportedFiles)> ExportAllDatabaseData(string exportDirectory)
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

                return (true, $"Successfully exported {exportedFiles.Count} files to {exportDirectory}", exportedFiles);
            }
            catch (Exception ex)
            {
                return (false, $"Export failed: {ex.Message}", new List<string>());
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
    }
}

namespace Nastya_Archiving_project.Services.SystemInfo
{
    public interface ISystemInfoServices
    {
        Task<string> GetLastRefNo();
        Task<(string? Id, string? error)> GetUserId();
        Task<(string? RealName, string? error)> GetRealName();
        Task<string?> GetUserIpAddress(); // Add this line
        Task<(bool Success, string Message, List<string> ExportedFiles)> ExportAllDatabaseData(string exportDirectory);
        Task<(bool Success, string Message, string BackupFilePath)> CreateAdvancedDatabaseBackup(string backupDirectory);
        Task<string> ExportTableToExcelAsync<T>(string filePath) where T : class;
    }
}

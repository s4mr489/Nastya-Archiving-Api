using iText.StyledXmlParser.Css.Resolve.Shorthand.Impl;
using Nastya_Archiving_project.Models.DTOs;

namespace Nastya_Archiving_project.Services.SystemInfo
{
    public interface ISystemInfoServices
    {
        Task<string> GetLastRefNo();
        Task<(string? Id, string? error)> GetUserId();
        Task<(string? RealName, string? error)> GetRealName();
        Task<string?> GetUserIpAddress(); // Add this line
        Task<(bool Success, string Message, string ExportFilePath, byte[] FileContent)> ExportAllDatabaseData(string exportDirectory);
        Task<(bool Success, string Message, string BackupFilePath, byte[] FileContent)> CreateAdvancedDatabaseBackup(string backupDirectory);
        Task<BaseResponseDTOs> GetbackupPath(int departId);
        Task<string> ExportTableToExcelAsync<T>(string filePath) where T : class;

        Task<bool> CheckUserHaveDepart(int departId, int userId);
        Task<BaseResponseDTOs> GetLastFourPartitions();
        Task<BaseResponseDTOs> GetIPartition();
        Task<BaseResponseDTOs> BackUpFiles(int point, bool backupAllFiles = false);
    }
}

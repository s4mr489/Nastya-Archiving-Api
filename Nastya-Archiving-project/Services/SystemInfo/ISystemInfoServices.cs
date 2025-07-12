namespace Nastya_Archiving_project.Services.SystemInfo
{
    public interface ISystemInfoServices
    {
        Task<string> GetLastRefNo();
        Task<(string? Id, string? error)> GetUserId();
        Task<(string? RealName, string? error)> GetRealName();
        Task<string?> GetUserIpAddress(); // Add this line
    }
}

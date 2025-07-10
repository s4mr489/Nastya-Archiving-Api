using Nastya_Archiving_project.Models.DTOs.UserInterface;

namespace Nastya_Archiving_project.Services.userInterface
{
    public interface IUserInterfaceServices
    {
        /// <summary>
        /// Program Interface Implementation
        /// that use to give the users selected permmissions to access the application
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<string> CreateUserInterface(UserInterfaceViewForm request);
        Task<(List<UserInterfaceResponseDTOs>? urls, string? error)> GetUserInterfaceForUser();
        Task<Dictionary<string, List<UserInterfaceResponseDTOs>>> GetPageUrlsGroupedByOutputType();
    }
}

using iText.StyledXmlParser.Css.Resolve.Shorthand.Impl;
using Nastya_Archiving_project.Models.DTOs;

namespace Nastya_Archiving_project.Services.home
{
    public interface IHomeServices
    {
        Task<BaseResponseDTOs> UsersCount();
        Task<BaseResponseDTOs> DocsCount();
        Task<BaseResponseDTOs> ActiveUsers();
        Task<BaseResponseDTOs> BranchCount();
        Task<BaseResponseDTOs> DocsAvaregByDay();
        Task<BaseResponseDTOs> DepartmentCount();
        Task<BaseResponseDTOs> TotalDocsSize();
        Task<BaseResponseDTOs> UserDocsByType(string timeFrame = null);

       
    }
}

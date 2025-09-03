using Nastya_Archiving_project.Models.DTOs;
using Nastya_Archiving_project.Models.DTOs.Limitation;
using System.Threading.Tasks;

namespace Nastya_Archiving_project.Services.Limitation
{
    public interface ILimitationServices
    {
        Task<BaseResponseDTOs> CreateEncryptedTextFile(LicenseCreationDTO licenseParams);
        Task<BaseResponseDTOs> ReadEncryptedTextFile();
    }
}

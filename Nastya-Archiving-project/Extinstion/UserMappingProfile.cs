using AutoMapper;
using Nastya_Archiving_project.Models;
using Nastya_Archiving_project.Models.DTOs.ArchivingDocs;
using Nastya_Archiving_project.Models.DTOs.Auth;

namespace Nastya_Archiving_project.Extinstion
{
    public class UserMappingProfile : Profile
    {
        public UserMappingProfile()
        {
            CreateMap<ArcivingDoc, ArchivingDocsResponseDTOs>();

            CreateMap<User, UsersResponseDTOs>()
                .ForMember(dest => dest.userName, opt => opt.MapFrom(src => src.UserName))
                .ForMember(dest => dest.realName, opt => opt.MapFrom(src => src.Realname))
                .ForMember(dest => dest.accountUnit, opt => opt.MapFrom(src => src.AccountUnitId.ToString()))
                .ForMember(dest => dest.branch, opt => opt.MapFrom(src => src.BranchId.ToString()))
                .ForMember(dest => dest.depart, opt => opt.MapFrom(src => src.DepariId.ToString()))
                .ForMember(dest => dest.jobTitl, opt => opt.MapFrom(src => src.JobTitle.ToString()))
                .ForMember(dest => dest.permission, opt => opt.MapFrom(src => src.Permtype));
        }

    }
}

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
             .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
             .ForMember(dest => dest.realName, opt => opt.MapFrom(src => src.Realname))
             .ForMember(dest => dest.userName, opt => opt.MapFrom(src => src.UserName))
             .ForMember(dest => dest.accountUnit, opt => opt.Ignore()) // Set in service if needed
             .ForMember(dest => dest.branch, opt => opt.Ignore())      // Set in service if needed
             .ForMember(dest => dest.depart, opt => opt.Ignore())      // Set in service if needed
             .ForMember(dest => dest.jobTitl, opt => opt.Ignore())     // Set in service if needed
             .ForMember(dest => dest.permission, opt => opt.MapFrom(src => src.Adminst));
        }
    }
}

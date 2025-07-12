using AutoMapper;
using Nastya_Archiving_project.Models;
using Nastya_Archiving_project.Models.DTOs.ArchivingDocs;

namespace Nastya_Archiving_project.Extinstion
{
    public class UserMappingProfile : Profile
    {
        public UserMappingProfile()
        {
            CreateMap<ArcivingDoc, ArchivingDocsResponseDTOs>();

        }

    }
}

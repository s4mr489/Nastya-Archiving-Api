using Nastya_Archiving_project.Models.DTOs.ArchivingDocs;

namespace Nastya_Archiving_project.Services.search
{
    public interface ISearchSerivces
    {
        Task<(List<ArchivingDocsResponseDTOs>? docs, string? error)> QuikSearch();
    }
}

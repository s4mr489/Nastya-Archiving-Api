using iText.Kernel.Actions.Events;
using Nastya_Archiving_project.Models.DTOs.ArchivingDocs;
using Nastya_Archiving_project.Models.DTOs.ArchivingDocs.Linkdocuments;
using Nastya_Archiving_project.Models.DTOs.file;

namespace Nastya_Archiving_project.Services.archivingDocs
{
    public interface IArchivingDocsSercvices
    {
        /// <summary>
        /// this implmention used for archiving operation docs 
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        Task<(ArchivingDocsResponseDTOs? docs, string? error)> PostArchivingDocs(ArchivingDocsViewForm req , FileViewForm file);
        Task<(ArchivingDocsResponseDTOs? docs, string? error)> EditArchivingDocs(ArchivingDocsViewForm req, int Id);
        Task<(List<ArchivingDocsResponseDTOs>? docs, string? error)> GetAllArchivingDocs();
        Task<(ArchivingDocsResponseDTOs? docs, string? error)> GetArchivingDocsById(int Id);
        Task<string> DeleteArchivingDocs(int Id);


        //That Implmentation used to like the document with each other by the refernce No 
        Task<(LinkdocumentsResponseDTOs? docs , string? error)> Linkdocuments(LinkdocumentsViewForm req, int Id);

        //That Implmentation used to restuor the DocsFrom the Delete Table
        Task<string> RestoreDeletedDocuments(int Id);
    }
}

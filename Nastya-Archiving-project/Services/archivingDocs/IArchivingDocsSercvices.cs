using iText.Kernel.Actions.Events;
using Nastya_Archiving_project.Models.DTOs;
using Nastya_Archiving_project.Models.DTOs.ArchivingDocs;
using Nastya_Archiving_project.Models.DTOs.ArchivingDocs.JoinedDocs;
using Nastya_Archiving_project.Models.DTOs.ArchivingDocs.Linkdocuments;
using Nastya_Archiving_project.Models.DTOs.file;
using Nastya_Archiving_project.Models.DTOs.Infrastruture.JobTitle;

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
        Task<string> DeleteArchivingDocs(int Id);


        //that Implementation used to remove the joined between the child and the parent document
        Task<(ArchivingDocsResponseDTOs? docs, string? error)> UnbindDoucFromTheArchive(string systemId);
        //That Implmentation used to like the document with each other by the refernce No 
        Task<(LinkdocumentsResponseDTOs? docs , string? error)> Linkdocuments(LinkdocumentsViewForm req, int Id);

        //That Implmentation used to restuor the DocsFrom the Delete Table
        Task<string> RestoreDeletedDocuments(int Id);

        /// <summary> this implmention used to Joined the docs from the archive </summary>
        Task<BaseResponseDTOs> JoinDocsFromArchive(JoinedDocsViewForm req);
        /// <summary>
        /// Gets image URLs from the ArcivingDocs entity with pagination support
        /// </summary>
        /// <param name="page">Page number (1-based)</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <param name="getLastImage">If true, returns only the last image URL</param>
        /// <param name="getFirstImage">If true, returns only the first image URL</param>
        /// <returns>A tuple with list of image URLs, error message if any, and total count</returns>
        Task<(List<ImageUrlDTO>? imageUrls, string? error, int totalCount)> GetArchivingDocImages(
            int page = 1,
            int pageSize = 10,
            bool getLastImage = false,
            bool getFirstImage = false);

        Task<BaseResponseDTOs> UnbindDoucAllDocsFromTheParent(string parentSystemId);
    }
}

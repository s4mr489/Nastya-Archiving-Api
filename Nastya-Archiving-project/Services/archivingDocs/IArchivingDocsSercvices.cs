using iText.Kernel.Actions.Events;
using Nastya_Archiving_project.Models.DTOs;
using Nastya_Archiving_project.Models.DTOs.ArchivingDocs;
using Nastya_Archiving_project.Models.DTOs.ArchivingDocs.JoinedDocs;
using Nastya_Archiving_project.Models.DTOs.ArchivingDocs.Linkdocuments;
using Nastya_Archiving_project.Models.DTOs.file;
using Nastya_Archiving_project.Models.DTOs.Infrastruture.JobTitle;
using Nastya_Archiving_project.Models.DTOs.Reports;

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
        //that Implementation used to remove the joined between the child and the parent document
        Task<(ArchivingDocsResponseDTOs? docs, string? error)> UnbindDoucFromTheArchive(string systemId);
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
        
        /// <summary>
        /// Gets a document by its reference number with all related details
        /// </summary>
        /// <param name="referenceNo">The reference number of the document to retrieve</param>
        /// <returns>A response with document details or an error message</returns>
        Task<BaseResponseDTOs> GetDocumentByReferenceNo(string referenceNo);
        
        Task<string> GetNextRefernceNo();
        Task<string> DeleteArchivingDocs(int Id);
        Task<string> RestoreDeletedDocuments(int Id);
        Task<BaseResponseDTOs> JoinDocsFromArchive(JoinedDocsViewForm req);
        Task<BaseResponseDTOs> UnbindDoucAllDocsFromTheParent(string parentSystemId);
        Task<(LinkdocumentsResponseDTOs? docs, string? error)> Linkdocuments(LinkdocumentsViewForm req, int Id);
        Task<string> GetAzberNo(string referneceNo);
    }
}

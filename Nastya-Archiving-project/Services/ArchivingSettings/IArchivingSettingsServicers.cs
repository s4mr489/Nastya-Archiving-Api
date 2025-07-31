using Nastya_Archiving_project.Models.DTOs.ArchivingSettings.ArchivingPoint;
using Nastya_Archiving_project.Models.DTOs.ArchivingSettings.DocsType;
using Nastya_Archiving_project.Models.DTOs.ArchivingSettings.Precedence;
using Nastya_Archiving_project.Models.DTOs.ArchivingSettings.SupDocsType;

namespace Nastya_Archiving_project.Services.ArchivingSettings
{
    public interface IArchivingSettingsServicers
    {
        /// <summary>
        /// this implmention used for archiving operation settings 
        /// </summary>
        /// 
        //Archiving Point Implementation
        Task<(ArchivingPointResponseDTOs? point , string? error)> PostArchivingPoint(ArchivingPointViewForm req);
        Task<(ArchivingPointResponseDTOs? point, string? error)> EditArchivingPoint(ArchivingPointViewForm req, int Id);
        Task<(List<ArchivingPointResponseDTOs>? points, string? error)> GetAllArchivingPoints();
        Task<(ArchivingPointResponseDTOs? point, string? error)> GetArchivingPointById(int Id);
        Task<string> DeleteArchivingPoint(int Id);


        //DocsType Implementation
        Task<(DocTypeResponseDTOs? docsType, string? error)> PostDocsType(DocTypeViewform req);
        Task<(DocTypeResponseDTOs? docsType, string? error)> EditDocsType(DocTypeViewform req, int Id);
        Task<(List<DocTypeResponseDTOs>? docsTypes, string? error)> GetAllDocsTypes();
        Task<(DocTypeResponseDTOs? docsType, string? error)> GetDocsTypeById(int Id);
        Task<(DocTypeResponseDTOs? docsType, string? error)> GetDocsTypeByDepartId(int DepartId);
        Task<string> DeleteDocsType(int Id);


        // Sup Docs Type Implementation

        Task<(SupDocsTypeResponseDTOs? supDocsType, string? error)> PostSupDocsType(SupDocsTypeViewform req);
        Task<(SupDocsTypeResponseDTOs? supDocsType, string? error)> EditSupDocsType(SupDocsTypeViewform req, int Id);
        Task<(List<SupDocsTypeResponseDTOs>? supDocsTypes, string? error)> GetAllSupDocsTypes();
        Task<(SupDocsTypeResponseDTOs? supDocsType, string? error)> GetSupDocsTypeById(int Id);
        Task<string> DeleteSupDocsType(int Id);

        //Precednce Implementation
        Task<(PrecedenceResponseDTOs? precednce, string? error)> PostPrecednce(PrecedenceViewForm req);
        Task<(PrecedenceResponseDTOs? precednce, string? error)> EditPrecednce(PrecedenceViewForm req, int Id);
        Task<(List<PrecedenceResponseDTOs>? precednces, string? error)> GetAllPrecednces();
        Task<(PrecedenceResponseDTOs? precednce, string? error)> GetPrecednceById(int Id);
        Task<string> DeletePrecednce(int Id);

    }
}

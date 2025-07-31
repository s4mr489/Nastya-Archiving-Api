using System.ComponentModel.DataAnnotations.Schema;

namespace Nastya_Archiving_project.Models.Entity
{
    [Table("T_JoinedDoc")]
    public class T_JoinedDoc
    {
        
       public int Id { get; set; }
       public string? ParentRefrenceNO { get; set; }
       public string? ChildRefrenceNo { get; set; }
       public int? BreafcaseNo { get; set; }
       public DateTime? editDate { get; set; }
    }
}

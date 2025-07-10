namespace Nastya_Archiving_project.Models
{
    public class BaseEntity
    {
        public int Id { get; set; }
        public  bool IsDeleted{ get; set; }
        public DateTime CreatedAt { get; set; } 
    }
}

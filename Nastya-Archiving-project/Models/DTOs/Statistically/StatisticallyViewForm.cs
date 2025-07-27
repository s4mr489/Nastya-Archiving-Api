namespace Nastya_Archiving_project.Models.DTOs.Statistically
{
    public class StatisticallyViewForm
    {
        public int? year { get; set; }
        public List<int?>? departmentId { get; set; }

        public int pageNumber { get; set; } = 1;
        public int pageSize { get; set; } = 20;
    }
}

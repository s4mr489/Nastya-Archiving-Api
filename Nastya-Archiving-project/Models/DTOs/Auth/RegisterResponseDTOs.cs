namespace Nastya_Archiving_project.Models.DTOs.Auth
{
    public class RegisterResponseDTOs
    {  
        public int Id { get; set; }

        public string? Realname { get; set; }

        public string? UserName { get; set; }

        public string? UserPassword { get; set; }

        public int? GroupId { get; set; }

        public string? Permtype { get; set; }

        public string? Adminst { get; set; }

        public string? Editor { get; set; }

        public DateOnly? EditDate { get; set; }

        public int? AccountUnitId { get; set; }

        public int? GobStep { get; set; }

        public int? DepariId { get; set; }

        public int? DevisionId { get; set; }

        public int? BranchId { get; set; }

        public int? AsWfuser { get; set; }

        public int? AsmailCenter { get; set; }

        public int? JobTitle { get; set; }
    }
}

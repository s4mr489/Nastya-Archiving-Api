namespace Nastya_Archiving_project.Models.DTOs.Auth
{
    public class RegisterViewForm
    {
        public string? Realname { get; set; }

        public string? UserName { get; set; }

        public string? UserPassword { get; set; }

        public int  GroupId { get; set; }

        public string? Permtype { get; set; }

       // public string? Adminst { get; set; }

        public string? Editor { get; set; }

        public DateOnly? EditDate { get; set; }

        public int AccountUnitId { get; set; }

     //   public int? GobStep { get; set; }

        public int DeparId { get; set; }

      //  public int? DevisionId { get; set; }

        public int BranchId { get; set; }

        // public int? AsWfuser { get; set; }

        //  public int? AsmailCenter { get; set; }

        public int JobTitle { get; set; }
        public string? Email { get; set; }
        public string? PhoneNo { get; set; }
        public string? Address { get; set; }
    }
}

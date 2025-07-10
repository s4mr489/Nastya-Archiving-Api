using Microsoft.VisualBasic;
using System.Text.Json.Serialization;

namespace Nastya_Archiving_project.Models.DTOs;

public class BaseDTO
{
    public int Id { get; set; }

}

public class BaseFormDTO
{

}

public class BaseUpdateDTO
{
    [JsonIgnore] public int Id { get; set; }
}

public class  BaseFilter
{
    public int? Id { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 15;
    public DateTime? EndDate { get; set; }
    public DateTime? EditDate { get; set; }
    public bool? IsDeleted { get; set; }
    public DateTime? StartDate { get; set; }
}
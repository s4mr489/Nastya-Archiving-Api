using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Nastya_Archiving_project.Models;

public partial class Usersgroup
{
    [Key]
    public int groupid { get; set; }

    public string? Groupdscrp { get; set; }

    public string? Editor { get; set; }

    public DateOnly? EditDate { get; set; }

    public int? AccountUnitId { get; set; }
}

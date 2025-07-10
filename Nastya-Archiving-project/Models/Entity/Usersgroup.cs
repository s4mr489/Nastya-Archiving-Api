using System;
using System.Collections.Generic;

namespace Nastya_Archiving_project.Models;

public partial class Usersgroup
{
    public int Id { get; set; }

    public string? Groupdscrp { get; set; }

    public string? Editor { get; set; }

    public DateOnly? EditDate { get; set; }

    public int? AccountUnitId { get; set; }
}

using System;
using System.Collections.Generic;

namespace Nastya_Archiving_project.Models;

public partial class TWritedDocument
{
    public int Id { get; set; }

    public string? DocNo { get; set; }

    public DateOnly? DocDate { get; set; }

    public string? Subject { get; set; }

    public string? Body { get; set; }

    public string? BoxfileNo { get; set; }

    public int? DocType { get; set; }

    public int? AccountUnitId { get; set; }

    public int? BranchId { get; set; }

    public int? DepartId { get; set; }

    public string? Editor { get; set; }

    public DateOnly? EditDate { get; set; }

    public int? Theyear { get; set; }

    public int? TheMonth { get; set; }
}

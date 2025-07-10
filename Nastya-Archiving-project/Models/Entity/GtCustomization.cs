using System;
using System.Collections.Generic;

namespace Nastya_Archiving_project.Models;

public partial class GtCustomization
{
    public int Id { get; set; }

    public string? TheTable { get; set; }

    public string? TheTableA { get; set; }

    public string? TheValue { get; set; }

    public string? TheType { get; set; }

    public int? AccountUnitId { get; set; }

    public int? BranchId { get; set; }

    public int? DepartId { get; set; }

    public int? DefaultValue { get; set; }
}

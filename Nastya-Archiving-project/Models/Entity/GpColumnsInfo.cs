using System;
using System.Collections.Generic;

namespace Nastya_Archiving_project.Models;

public partial class GpColumnsInfo
{
    public int Id { get; set; }

    public string? Dscrp { get; set; }

    public string? DscrpA { get; set; }

    public string? TblName { get; set; }

    public string? TheSystem { get; set; }

    public string? DataType { get; set; }

    public int? UserInserted { get; set; }

    public string? ParameterTable { get; set; }

    public int? ControlType { get; set; }

    public int? AccountUnitId { get; set; }

    public int? BranchId { get; set; }

    public int? DepartmentId { get; set; }

    public int? Required { get; set; }
}

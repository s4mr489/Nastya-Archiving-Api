using System;
using System.Collections.Generic;

namespace Nastya_Archiving_project.Models;

public partial class UsersReportColumn
{
    public int Id { get; set; }

    public int? PrimeryKey { get; set; }

    public string? ColumnName { get; set; }

    public string? ColumnNamedisplay { get; set; }

    public string? TblName { get; set; }

    public string? TblNameA { get; set; }

    public string? TheSystem { get; set; }

    public string? DataType { get; set; }

    public int? UserInserted { get; set; }

    public int? Seq { get; set; }

    public string? JoiningPart { get; set; }

    public string? ConStr { get; set; }

    public string? ControlType { get; set; }

    public int? AccountUnitId { get; set; }

    public int? BranchId { get; set; }

    public int? DepartmentId { get; set; }

    public int? Required { get; set; }

    public string? ParameterTable { get; set; }

    public int? Asparameter { get; set; }

    public string? SecondaryTable { get; set; }

    public string? SecondaryKey { get; set; }
}

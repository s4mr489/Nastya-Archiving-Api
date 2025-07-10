using System;
using System.Collections.Generic;

namespace Nastya_Archiving_project.Models;

public partial class UsersEditing
{
    public int Id { get; set; }

    public string? Model { get; set; }

    public string? TblName { get; set; }

    public string? TblNameA { get; set; }

    public string? RecordId { get; set; }

    public string? RecordData { get; set; }

    public string? OperationType { get; set; }

    public int? AccountUnitId { get; set; }

    public string? Editor { get; set; }

    public DateTime? EditDate { get; set; }

    public string? Ipadress { get; set; }
}

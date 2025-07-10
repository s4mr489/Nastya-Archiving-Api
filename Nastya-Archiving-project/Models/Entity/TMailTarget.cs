using System;
using System.Collections.Generic;

namespace Nastya_Archiving_project.Models;

public partial class TMailTarget
{
    public int Id { get; set; }

    public string? Username { get; set; }

    public string? UsertargetMail { get; set; }

    public int? DepartmentId { get; set; }
}

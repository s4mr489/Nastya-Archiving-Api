using System;
using System.Collections.Generic;

namespace Nastya_Archiving_project.Models;

public partial class ArcivSubDocDscrp
{
    public int Id { get; set; }

    public string? Dscrp { get; set; }

    public int? DocTypeId { get; set; }
    public int? accountUnitId { get; set; }
    public int? branchId { get; set; }
    public int? departId { get; set; }
}

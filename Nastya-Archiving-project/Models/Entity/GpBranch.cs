using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Nastya_Archiving_project.Models;

public partial class GpBranch
{
    [Key]
    public int Id { get; set; }

    public string? Dscrp { get; set; }

    public int? AccountUnitId { get; set; }
}

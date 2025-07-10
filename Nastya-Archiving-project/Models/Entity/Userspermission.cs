using System;
using System.Collections.Generic;

namespace Nastya_Archiving_project.Models;

public partial class Userspermission
{
    public int Id { get; set; }

    public int? Groupid { get; set; }

    public string? Pageid { get; set; }

    public int? AccountUnitId { get; set; }
}

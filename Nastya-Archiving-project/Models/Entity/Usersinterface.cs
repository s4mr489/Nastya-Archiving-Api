using System;
using System.Collections.Generic;

namespace Nastya_Archiving_project.Models;

public partial class Usersinterface
{
    public int Id { get; set; }

    public string? Pagedscrp { get; set; }

    public string? Pageurl { get; set; }

    public string? Outputtype { get; set; }

    public string? Program { get; set; }

    public int? Serials { get; set; }

    public int? AccountUnitId { get; set; }
}

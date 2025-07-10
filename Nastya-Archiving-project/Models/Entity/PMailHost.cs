using System;
using System.Collections.Generic;

namespace Nastya_Archiving_project.Models;

public partial class PMailHost
{
    public int Id { get; set; }

    public string? Dscrp { get; set; }

    public string? ServerDomen { get; set; }

    public int? ServerPort { get; set; }

    public string? EMail { get; set; }

    public string? Pass { get; set; }

    public string? EnabledSsl { get; set; }
}

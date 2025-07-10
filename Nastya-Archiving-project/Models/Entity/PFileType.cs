using System;
using System.Collections.Generic;

namespace Nastya_Archiving_project.Models;

public partial class PFileType
{
    public int Id { get; set; }

    public string? Dscrp { get; set; }

    public string? Extention { get; set; }

    public int? Code { get; set; }
}

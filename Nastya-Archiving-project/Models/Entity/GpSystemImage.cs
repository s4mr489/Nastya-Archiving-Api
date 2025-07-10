using System;
using System.Collections.Generic;

namespace Nastya_Archiving_project.Models;

public partial class GpSystemImage
{
    public int Id { get; set; }

    public string? Dscrp { get; set; }

    public byte[]? Img { get; set; }

    public int? AccountUnitId { get; set; }
}

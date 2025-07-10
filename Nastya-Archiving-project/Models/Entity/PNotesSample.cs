using System;
using System.Collections.Generic;

namespace Nastya_Archiving_project.Models;

public partial class PNotesSample
{
    public int Id { get; set; }

    public string? Dscrp { get; set; }

    public string? Notes { get; set; }

    public int? UserId { get; set; }
}

using System;
using System.Collections.Generic;

namespace Nastya_Archiving_project.Models;

public partial class PMailSample
{
    public int Id { get; set; }

    public string? Subject { get; set; }

    public string? Dscrp { get; set; }

    public int? UserId { get; set; }
}

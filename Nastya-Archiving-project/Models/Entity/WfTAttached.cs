using System;
using System.Collections.Generic;

namespace Nastya_Archiving_project.Models;

public partial class WfTAttached
{
    public int Id { get; set; }

    public int? CaseId { get; set; }

    public int? CaseSequence { get; set; }

    public int? UserId { get; set; }

    public string? ImgUrl { get; set; }

    public string? FileNam { get; set; }

    public string? RefrenceNo { get; set; }
}

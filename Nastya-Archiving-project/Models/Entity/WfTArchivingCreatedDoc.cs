using System;
using System.Collections.Generic;

namespace Nastya_Archiving_project.Models;

public partial class WfTArchivingCreatedDoc
{
    public int Id { get; set; }

    public int? CaseId { get; set; }

    public int? DocId { get; set; }

    public string? DocNo { get; set; }

    public DateOnly? DocDate { get; set; }

    public int? SourceId { get; set; }

    public int? TargetId { get; set; }

    public string? Subject { get; set; }

    public string? Body { get; set; }

    public int? AccountUnitId { get; set; }

    public int? BranchId { get; set; }

    public int? DepartId { get; set; }

    public string? Editor { get; set; }

    public DateOnly? EditDate { get; set; }

    public int? Theyear { get; set; }

    public int? Themonth { get; set; }

    public int? ProcessType { get; set; }

    public string? ImgUrl { get; set; }

    public string? Ipaddress { get; set; }

    public string? DocIdtype { get; set; }
}

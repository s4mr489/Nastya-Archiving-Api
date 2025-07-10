using System;
using System.Collections.Generic;

namespace Nastya_Archiving_project.Models;

public partial class WfTProcessing
{
    public int Id { get; set; }

    public int? CaseId { get; set; }

    public int? PrecedenceId { get; set; }

    public string? RefrenceNo { get; set; }

    public int? CaseCreatedBy { get; set; }

    public int? CaseTarget { get; set; }

    public string? Subject { get; set; }

    public int? CaseClosed { get; set; }

    public int? DocType { get; set; }

    public int? DocId { get; set; }

    public string? DocNo { get; set; }

    public DateOnly? DocDate { get; set; }

    public int? ProcessType { get; set; }

    public string? Notes { get; set; }

    public int? Theyear { get; set; }

    public int? Themonth { get; set; }

    public int? AccountUnitId { get; set; }

    public int? BranchId { get; set; }

    public int? DepartId { get; set; }
}

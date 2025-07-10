using System;
using System.Collections.Generic;

namespace Nastya_Archiving_project.Models;

public partial class WfTExportDocNo
{
    public int Id { get; set; }

    public string? ExportDocNo { get; set; }

    public string? Editor { get; set; }

    public DateOnly? EditDate { get; set; }

    public int? Theyear { get; set; }

    public int? AccountUnitId { get; set; }

    public int? ProcessTypeId { get; set; }
}

using System;
using System.Collections.Generic;

namespace Nastya_Archiving_project.Models;

public partial class ArcivDocsRefrence
{
    public int Id { get; set; }

    public string? HeadReferenceNo { get; set; }

    public string? LinkedRfrenceNo { get; set; }

    public string? Dscription { get; set; }

    public int? PakegId { get; set; }

    public int? AccountUnitId { get; set; }

    public string? Editor { get; set; }

    public DateOnly? EditDate { get; set; }
}

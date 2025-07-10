using System;
using System.Collections.Generic;

namespace Nastya_Archiving_project.Models;

public partial class TMailArchiving
{
    public int Id { get; set; }

    public string? Subject { get; set; }

    public string? BodyStr { get; set; }

    public string? SenderName { get; set; }

    public DateOnly? SendDate { get; set; }

    public string? SendTo { get; set; }

    public string? Ccto { get; set; }

    public string? FileUrl { get; set; }

    public int? HostAdress { get; set; }

    public int? AccountUnitId { get; set; }
}

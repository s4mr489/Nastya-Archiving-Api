using System;
using System.Collections.Generic;

namespace Nastya_Archiving_project.Models;

public partial class TFileTransferring
{
    public int Id { get; set; }

    public string? RefrenceNo { get; set; }

    public string? From { get; set; }

    public string? To { get; set; }

    public string? Notes { get; set; }

    public DateTime? SendDate { get; set; }

    public int? Readed { get; set; }
}

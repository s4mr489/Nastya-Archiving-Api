using System;
using System.Collections.Generic;

namespace Nastya_Archiving_project.Models;

public partial class WfProcessSequence
{
    public int Id { get; set; }

    public int? CaseId { get; set; }

    public int? CaseSequence { get; set; }

    public int? FromUserId { get; set; }

    public string? FromUserName { get; set; }

    public int? ToUserId { get; set; }

    public string? ToUserName { get; set; }

    public DateTime? SendDate { get; set; }

    public string? Notes { get; set; }

    public DateTime? OpeninigDate { get; set; }

    public byte[]? Signature { get; set; }

    public int? Approved { get; set; }

    public int? Cc { get; set; }

    public int? Redirected { get; set; }

    public int? UnderHand { get; set; }
}

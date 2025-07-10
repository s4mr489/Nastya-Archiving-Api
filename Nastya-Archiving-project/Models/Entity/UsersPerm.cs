using System;
using System.Collections.Generic;

namespace Nastya_Archiving_project.Models;

public partial class UsersPerm
{
    public int Id { get; set; }

    public string? PermType { get; set; }

    public int? PermId { get; set; }
}

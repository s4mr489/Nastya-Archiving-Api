using System;
using System.Collections.Generic;

namespace Nastya_Archiving_project.Models;

public partial class UsersDoctypePermission
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public int? DoctypeId { get; set; }
}

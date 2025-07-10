using System;
using System.Collections.Generic;

namespace Nastya_Archiving_project.Models;

public partial class UsersOptionPermission
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public int? AddParameters { get; set; }

    public int? AllowDelete { get; set; }

    public int? AllowAddToOther { get; set; }

    public int? AllowViewTheOther { get; set; }

    public int? AllowSendMail { get; set; }

    public int? AllowDownload { get; set; }
}

using System;
using System.Collections.Generic;

namespace Nastya_Archiving_project.Models;

public partial class UsersFileBrowing
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public int? FileType { get; set; }
}

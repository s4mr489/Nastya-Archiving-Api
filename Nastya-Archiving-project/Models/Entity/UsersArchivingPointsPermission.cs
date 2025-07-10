using System;
using System.Collections.Generic;

namespace Nastya_Archiving_project.Models;

public partial class UsersArchivingPointsPermission
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public int? ArchivingpointId { get; set; }

    public int? AccountUnitId { get; set; }
}

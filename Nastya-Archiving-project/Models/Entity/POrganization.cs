﻿using System;
using System.Collections.Generic;

namespace Nastya_Archiving_project.Models;

public partial class POrganization
{
    public int Id { get; set; }

    public string? Dscrp { get; set; }

    public int? DepartId { get; set; }

    public int? AccountUnitId { get; set; }

    public int? BranchId { get; set; }
}

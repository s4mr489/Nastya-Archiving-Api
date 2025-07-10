using System;
using System.Collections.Generic;

namespace Nastya_Archiving_project.Models;

public partial class TDocumentsRelation
{
    public int Id { get; set; }

    public int? ParentDocId { get; set; }

    public int? RelatedDocId { get; set; }
}

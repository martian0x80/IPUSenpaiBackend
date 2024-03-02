using System;
using System.Collections.Generic;

namespace IPUSenpaiBackend.Entities;

public partial class Student
{
    public string? Name { get; set; }

    public string? Sid { get; set; }

    public short? Instcode { get; set; }

    public short? Batch { get; set; }

    public string? Progcode { get; set; }

    public string Enrolno { get; set; } = null!;

    public virtual Institute? InstcodeNavigation { get; set; }

    public virtual Programme? ProgcodeNavigation { get; set; }

    public virtual ICollection<Result> Results { get; set; } = new List<Result>();
}

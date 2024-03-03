using System;
using System.Collections.Generic;

namespace IPUSenpaiBackend.Entities;

public partial class Programme
{
    public string Progcode { get; set; } = null!;

    public string? Progname { get; set; }

    public string? Prog { get; set; }

    public string? Spec { get; set; }

    public virtual ICollection<Student> Students { get; set; } = new List<Student>();
}

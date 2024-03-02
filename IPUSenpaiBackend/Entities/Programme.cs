using System;
using System.Collections.Generic;

namespace IPUSenpaiBackend.Entities;

public partial class Programme
{
    public string Progcode { get; set; } = null!;

    public string? Progname { get; set; }

    public virtual ICollection<Student> Students { get; set; } = new List<Student>();
}

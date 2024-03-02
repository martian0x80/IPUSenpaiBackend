using System;
using System.Collections.Generic;

namespace IPUSenpaiBackend.Entities;

public partial class Institute
{
    public short Instcode { get; set; }

    public string? Instname { get; set; }

    public virtual ICollection<Student> Students { get; set; } = new List<Student>();
}

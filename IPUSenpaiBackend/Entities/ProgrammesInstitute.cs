using System;
using System.Collections.Generic;

namespace IPUSenpaiBackend.Entities;

public partial class ProgrammesInstitute
{
    public string? Progcode { get; set; }

    public short? Instcode { get; set; }

    public virtual Institute? InstcodeNavigation { get; set; }

    public virtual Programme? ProgcodeNavigation { get; set; }
}

using System;
using System.Collections.Generic;

namespace IPUSenpaiBackend.Entities;

public partial class Result
{
    public int ResultId { get; set; }

    public string Enrolno { get; set; } = null!;

    public string Subcode { get; set; } = null!;

    public string Schemeid { get; set; } = null!;

    public short? Internal { get; set; }

    public short? External { get; set; }

    public short? Total { get; set; }

    public short Semester { get; set; }

    public string Exam { get; set; } = null!;

    public short? Batch { get; set; }

    public string? Resultdate { get; set; }

    public virtual Student EnrolnoNavigation { get; set; } = null!;
}

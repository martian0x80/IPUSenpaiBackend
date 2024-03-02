using System;
using System.Collections.Generic;

namespace IPUSenpaiBackend.Entities;

public partial class Subject
{
    public string Subcode { get; set; } = null!;

    public string Paperid { get; set; } = null!;

    public string? Papername { get; set; }

    public short? Credits { get; set; }

    public short? Minor { get; set; }

    public short? Major { get; set; }

    public string? Mode { get; set; }

    public string? Kind { get; set; }

    public short? Maxmarks { get; set; }

    public short? Passmarks { get; set; }

    public string Schemeid { get; set; } = null!;

    public string? Type { get; set; }

    public string? Exam { get; set; }

    public long Subid { get; set; }
}

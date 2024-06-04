namespace IPUSenpaiBackend.CustomEntities;

public class SubjectSenpai
{
    public string? Subcode { get; set; }
    public string? Paperid { get; set; }
    public string? Papername { get; set; }
    public short? Passmarks { get; set; }
    public short? Maxmarks { get; set; }
    public short? Credits { get; set; }
}

public class SubjectSenpaiFull
{
    public string? Subcode { get; set; }
    public string? Paperid { get; set; }
    public string? Papername { get; set; }
    public short? Credits { get; set; }
    public short? Minor { get; set; }
    public short? Major { get; set; }
    public string? Mode { get; set; }
    public string? Kind { get; set; }
    public short? Maxmarks { get; set; }
    public short? Passmarks { get; set; }
    public string? Schemeid { get; set; }
    public string? Type { get; set; }
    public string? Exam { get; set; }
}
namespace IPUSenpaiBackend.CustomEntities;

public class StudentSenpai
{
    public string? Name { get; set; }

    public string? Sid { get; set; }

    public short? Instcode { get; set; }

    public short? Batch { get; set; }

    public string? Progcode { get; set; }

    public string Enrolno { get; set; } = null!;
    
    public string? Institute { get; set; }
    
    public string? Programme { get; set; }
    
}
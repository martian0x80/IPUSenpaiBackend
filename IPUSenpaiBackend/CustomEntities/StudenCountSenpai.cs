namespace IPUSenpaiBackend.CustomEntities;

public class StudentCountSingle
{
    public string? Name { get; set; }
    public int Count { get; set; }
}

public class StudentCountByProgramme : IStudentCountByFilter
{
    public string? Name { get; set; } = "Programme";
    public List<StudentCountSingle>? StudentCounts { get; set; }
}

public class StudentCountByInstitute : IStudentCountByFilter
{
    public string? Name { get; set; } = "Institute";
    public List<StudentCountSingle>? StudentCounts { get; set; }
}

public class StudentCountByBatch : IStudentCountByFilter
{
    public string? Name { get; set; } = "Batch";
    public List<StudentCountSingle>? StudentCounts { get; set; }
}

public class StudentCountBy
{
    public StudentCountByProgramme? ByProgramme { get; set; }
    public StudentCountByInstitute? ByInstitute { get; set; }
    public StudentCountByBatch? ByBatch { get; set; }
}
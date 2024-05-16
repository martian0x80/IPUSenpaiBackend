namespace IPUSenpaiBackend.CustomEntities;

public interface IStudentCountByFilter
{
    public string? Name { get; set; }
    public List<StudentCountSingle> StudentCounts { get; set; }
}
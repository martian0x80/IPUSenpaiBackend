using IPUSenpaiBackend.CustomEntities;

namespace IPUSenpaiBackend.IPUSenpai;

public class Response
{
    public string? Name { get; set; }
    public string? Value { get; set; }
}

public class PartialResponse
{
    public string? Name { get; set; }
}

public interface IRankSenpaiResponse
{
    public float AvgGpa { get; set; }
    public List<float>? GpaList { get; set; }
}

public class RankSenpaiOverallResponse : IRankSenpaiResponse
{
    public List<RankSenpaiOverall>? Ranklist { get; set; }
    public float AvgGpa { get; set; }
    public List<float>? GpaList { get; set; }
}

public class RankSenpaiSemesterResponse : IRankSenpaiResponse
{
    public List<RankSenpaiSemester>? Ranklist { get; set; }
    public float AvgGpa { get; set; }
    public List<float>? GpaList { get; set; }
}
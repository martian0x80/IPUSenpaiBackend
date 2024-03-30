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

public class GpaListResponse
{
    public string? Enrollment { get; set; }
    public string? Name { get; set; }
    public float Gpa { get; set; }
    public float Percentage { get; set; }
}

public class RankSenpaiResponse
{
    public float AvgGpa { get; set; }
    public float AvgPercentage { get; set; }
    public List<GpaListResponse>? GpaList { get; set; }
}

public class RankSenpaiOverallResponse : RankSenpaiResponse
{
    public List<RankSenpaiOverall>? Ranklist { get; set; }
}

public class RankSenpaiSemesterResponse : RankSenpaiResponse
{
    public List<RankSenpaiSemester>? Ranklist { get; set; }
}
namespace IPUSenpaiBackend.CustomEntities;

public class RankSenpaiOverall
{
    public string? Enrollment { get; set; }
    public string? Name { get; set; }
    public int Marks { get; set; }
    public int CreditMarks { get; set; }
    public int TotalCreditMarks { get; set; }
    public int TotalCreditMarksWeighted { get; set; }
    public int TotalCredits { get; set; }
    public int Total { get; set; }
    public float Cgpa { get; set; }
    public float Percentage { get; set; }
    public float CreditsPercentage { get; set; }
    public int Semesters { get; set; }
    public int Rank { get; set; }
    public List<Dictionary<string, string>> SgpaAllSem { get; set; }
    public List<Dictionary<string, int>> MarksPerSemester { get; set; }
}

public class RankSenpaiSemester
{
    public string? Enrollment { get; set; }
    public string? Name { get; set; }
    public int Marks { get; set; }
    public int CreditMarks { get; set; }
    public int TotalCreditMarks { get; set; }
    public int TotalCreditMarksWeighted { get; set; }
    public int TotalCredits { get; set; }
    public int Total { get; set; }
    public float Sgpa { get; set; }
    public int Rank { get; set; }
    public float Percentage { get; set; }
    public float CreditsPercentage { get; set; }
    public List<Dictionary<string, string>>? Subject { get; set; }
}
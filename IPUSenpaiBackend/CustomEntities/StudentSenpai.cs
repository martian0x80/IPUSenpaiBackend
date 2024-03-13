using System.Collections.Concurrent;

namespace IPUSenpaiBackend.CustomEntities;

public class StudentSenpai
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
    //public List<Dictionary<string, string>> SgpaAllSem { get; set; }
    public ConcurrentBag<ConcurrentDictionary<string, string>>? SgpaAllSem { get; set; }
    public ConcurrentBag<ConcurrentDictionary<string, int>> MarksPerSemester { get; set; }
    // public List<Dictionary<string, List<Dictionary<string, string>>>>? Subject { get; set; } 
    public ConcurrentBag<ConcurrentDictionary<string, object>>? Subject { get; set; }
    public ConcurrentBag<ConcurrentDictionary<string, string>>? CgpaBySem { get; set; }
    public ConcurrentBag<ConcurrentDictionary<string, string>>? CumulativePercentageBySem { get; set; }
    public ConcurrentBag<ConcurrentDictionary<string, string>>? CgpaByYear { get; set; }
    public ConcurrentBag<ConcurrentDictionary<string, string>>? CumulativePercentageByYear { get; set; }
}
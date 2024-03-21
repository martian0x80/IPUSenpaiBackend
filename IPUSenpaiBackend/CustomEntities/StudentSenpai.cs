namespace IPUSenpaiBackend.CustomEntities;

public class StudentSearchSenpai
{
    public string? Enrollment { get; set; } = "696969696969";
    public string? Name { get; set; } = "No results";
    public string? Institute { get; set; } = "Jujutsu Tech";
    public string? Programme { get; set; } = "B.Tech";
    public string? Batch { get; set; } = "2022";
}

public class StudentSearchFilterOptionsSenpai
{
    public string? Name { get; set; }
    public string? Institute { get; set; }
    public string? Programme { get; set; }
    public string? Batch { get; set; }
}

public class StudentSenpai
{
    public string? Enrollment { get; set; }
    public string? Name { get; set; }
    public string? Programme { get; set; }
    public string? Institute { get; set; }
    public string? Batch { get; set; }
    public string? Sid { get; set; }
    public string? InstCode { get; set; }
    public string? ProgCode { get; set; }
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

    //public List<Dictionary<string, string>>? SgpaAllSem { get; set; }

    public List<Dictionary<string, string>>? MarksPerSemester { get; set; }

    // public List<Dictionary<string, List<Dictionary<string, string>>>>? Subject { get; set; } 
    public List<Dictionary<string, object>>? Subject { get; set; }

    public List<Dictionary<string, string>>? CumulativeResult { get; set; }
    // public List<Dictionary<string, string>>? CumulativePercentageBySem { get; set; }
    // public List<Dictionary<string, string>>? CgpaByYear { get; set; }
    // public List<Dictionary<string, string>>? CumulativePercentageByYear { get; set; }
}
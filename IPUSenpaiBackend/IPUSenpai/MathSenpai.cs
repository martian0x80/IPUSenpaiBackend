namespace IPUSenpaiBackend.IPUSenpai;

public class MathSenpai
{
    public static string GetGrade(int score)
    {
        Dictionary<int, string> gradeMap = new Dictionary<int, string>
        {
            { 90, "O" },
            { 75, "A+" },
            { 65, "A" },
            { 55, "B+" },
            { 50, "B" },
            { 45, "C" },
            { 40, "P" },
            { 0, "F" }
        };

        foreach (var entry in gradeMap.OrderByDescending(entry => entry.Key))
        {
            if (score >= entry.Key)
                return entry.Value;
        }

        return "Invalid score";
    }
    
    public static int GetGradePoint(int score)
    {
        Dictionary<int, int> gradeMap = new Dictionary<int, int>
        {
            { 90, 10 },
            { 75, 9 },
            { 65, 8 },
            { 55, 7 },
            { 50, 6 },
            { 45, 5 },
            { 40, 4 },
            { 0, 0 }
        };

        foreach (var entry in gradeMap.OrderByDescending(entry => entry.Key))
        {
            if (score >= entry.Key)
                return entry.Value;
        }

        return 0;
    }
    
    public static float GetSgpa(int totalCreditsMarksWeighted, int totalCredits)
    {
        if (totalCredits == 0)
            return 0;
        return (float)totalCreditsMarksWeighted / totalCredits;
    }
    
    public static float GetCgpa(float weightedsgpa, int totalCreditsOverall)
    {
        if (totalCreditsOverall == 0)
            return 0;
        return (float)weightedsgpa / totalCreditsOverall;
    }
    
}
using IPUSenpaiBackend.CustomEntities;
using IPUSenpaiBackend.DBContext;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace IPUSenpaiBackend.IPUSenpai;

[AttributeUsage(AttributeTargets.Field)]
public sealed class StringValueAttribute : Attribute
{
    public StringValueAttribute(string value)
    {
        Value = value;
    }

    public string Value { get; }
}
public static class EnumExtensions
{
    public static string StringValue<T>(this T value)
        where T : Enum
    {
        var fieldName = value.ToString();
        var field = typeof(T).GetField(fieldName, BindingFlags.Public | BindingFlags.Static);
        return field?.GetCustomAttribute<StringValueAttribute>()?.Value ?? fieldName;
    }
}

public class IPUSenpaiAPI : IIPUSenpaiAPI
{
    private readonly IPUSenpaiDBContext _context;
    private readonly ILogger _logger;

    public IPUSenpaiAPI(IPUSenpaiDBContext context, ILogger<IPUSenpaiAPI> logger)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<StudentSenpai> GetStudentByEnrollment(string? enrollment)
    {
        StudentSenpai student = await _context.Students
            .Include(s => s.InstcodeNavigation)
            .Include(s => s.ProgcodeNavigation)
            .Where(s => s.Enrolno == enrollment)
            .Select(s => new StudentSenpai
            {
                Enrolno = s.Enrolno,
                Name = s.Name,
                Instcode = s.Instcode,
                Institute = s.InstcodeNavigation.Instname,
                Progcode = s.Progcode,
                Programme = s.ProgcodeNavigation.Progname,
                Batch = s.Batch,
                Sid = s.Sid,
            }).FirstOrDefaultAsync();

        if (student == null)
        {
            student = new StudentSenpai();
        }

        return student;
    }

    public async Task<List<Response>> GetInstitutes(short limit = 100)
    {
        /*
         * select instname, count(*) from institute inner join student on student.instcode=institute.instcode group by institute.instname having count(*) > 100 order by count(*) desc;
         */
        var institutes = await _context.Students
            .Include(s => s.InstcodeNavigation)
            .GroupBy(s => s.InstcodeNavigation.Instname)
            .Where(s => s.Count() > 100)
            .OrderByDescending(s => s.Count())
            .Select(s => new Response
            {
                Name = s.Key,
                Value = s.FirstOrDefault().InstcodeNavigation.Instcode.ToString()
            })
            .ToListAsync();
        // Total 107 unique institutes
        // var institutes = (from s in _context.Institutes
        //     join i in _context.Students on s.Instcode equals i.Instcode
        //     group s by s.Instname into g
        //     // where g.Count() > 500
        //     orderby g.Count() descending
        //     select g.Key).Take(limit);
        return institutes;
    }

    public async Task<List<PartialResponse>> GetInstitutesByProgramme(string programme, short limit = 100)
    {
        var programmes = await _context.ProgrammesInstitutes
            .Include(pi => pi.InstcodeNavigation)
            .Where(pi => pi.ProgcodeNavigation.Prog == programme)
            .GroupBy(pi => pi.InstcodeNavigation.Instname)
            .OrderBy(pi => pi.Key)
            .Select(pi => new PartialResponse
            {
                Name = pi.Key
            })
            // .OrderBy(instname => instname.Name)
            .ToListAsync();

        if (programmes.Count == 0)
        {
            programmes.Add(new PartialResponse
            {
                Name = "No institutes found",
            });
        }

        return programmes;
    }

    public async Task<List<PartialResponse>> GetProgrammes(short limit = 79)
    {
        var programmes = await _context.Programmes
            .GroupBy(p => p.Prog)
            .OrderBy(p => p.Key)
            .Select(p => new PartialResponse
            {
                Name = p.Key
            })
            // .OrderBy(p => p.Name)
            .Take(limit)
            .ToListAsync();
        if (programmes.Count == 0)
        {
            programmes.Add(new PartialResponse
            {
                Name = "No programmes found",
            });
        }

        return programmes;
    }

    public async Task<List<Response>> GetSpecializationsByProgrammeAndInstname(short limit = 30,
        string? prog = "BACHELOR OF TECHNOLOGY",
        string? instname = "University School of Information & Communication Technology")
    {
        // var specializations = await _context.Programmes
        // .Where(p => p.Prog == prog)
        // .Select(p => new
        // {
        //     p.Spec,
        //     p.Progcode
        // })
        // .Take(limit)
        // .ToDictionaryAsync(p => p.Spec, p => p.Progcode);

        var specializations = await _context.ProgrammesInstitutes
            .Where(pi => pi.ProgcodeNavigation.Prog == prog && pi.InstcodeNavigation.Instname == instname)
            .OrderBy(pi => pi.ProgcodeNavigation.Spec)
            .Select(pi => new Response
            {
                Name = pi.ProgcodeNavigation!.Spec,
                Value = pi.Progcode
            })
            .Distinct()
            .ToListAsync();

        if (specializations.Count == 0)
        {
            return new List<Response>
            {
                new Response
                {
                    Name = "No specializations found",
                    Value = "No specializations found"
                }
            };
        }

        return specializations;
    }

    public async Task<List<Response>> GetInstituteCodesForShifts(string instname)
    {
        var institutes = await _context.Institutes
            .Where(i => i.Instname == instname)
            .Select(i => i.Instcode)
            .OrderBy(s => s)
            .ToListAsync();

        List<Response> shifts = new();

        /*
         * Assign shifts to each institute code, the smallest institute code gets the morning shift
         * and the next smallest gets the evening shift
         * and the last one gets the other shift
         */
        if (institutes.Count > 0)
        {
            shifts.Add(new Response
            {
                Name = "Morning",
                Value = institutes[0].ToString()
            });
            if (institutes.Count > 1)
            {
                shifts.Add(new Response
                {
                    Name = "Evening",
                    Value = institutes[1].ToString()
                });
            }

            if (institutes.Count > 2)
            {
                shifts.Add(new Response
                {
                    Name = "Other",
                    Value = institutes[2].ToString()
                });
            }
        }

        return shifts;
    }

    public async Task<List<short?>> GetBatchesByPrognameAndInstname(string programme, string institute)
    {
        var batches = await _context.Students
            .Where(s => s.ProgcodeNavigation.Prog == programme && s.InstcodeNavigation.Instname == institute)
            .Select(s => s.Batch)
            .Distinct()
            .OrderByDescending(s => s)
            .ToListAsync();
        return batches;
    }

    public async Task<InstituteSenpai> GetInstituteByInstcode(short? instcode)
    {
        var institute = await _context.Institutes
            .Where(i => i.Instcode == instcode)
            .Select(i => new InstituteSenpai
            {
                Instcode = i.Instcode,
                Instname = i.Instname,
            })
            .Take(1)
            .FirstOrDefaultAsync();
        return institute;
    }

    public async Task<ProgrammeSenpai> GetProgrammeByProgcode(string? progcode)
    {
        var programme = await _context.Programmes
            .Where(p => p.Progcode == progcode)
            .Select(p => new ProgrammeSenpai
            {
                Progcode = p.Progcode,
                Progname = p.Progname,
                Prog = p.Prog,
                Spec = p.Spec
            })
            .Take(1)
            .FirstOrDefaultAsync();
        return programme;
    }

    public async Task<List<PartialResponse>> GetSemestersByProgrammeAndInstname(string programme, string institute)
    {
        var semesters = await _context.Results
            .Include(r => r.EnrolnoNavigation)
            .Where(r => r.EnrolnoNavigation.ProgcodeNavigation.Prog == programme &&
                        r.EnrolnoNavigation.InstcodeNavigation.Instname == institute)
            .GroupBy(r => r.Semester)
            .Select(r => new PartialResponse
            {
                Name = r.Key.ToString()
            })
            .OrderBy(r => r.Name)
            .ToListAsync();

        return semesters;
    }

    public async Task<Dictionary<string, Dictionary<string, string>>> GetSubjectsByEnrollment(string? enrollment)
    {
        /* To get subject details
         * select r.subcode, paperid, papername, passmarks, maxmarks, credits from results r left join subjects s on s.subcode = r.subcode full outer join student st on st.enrolno=r.enrolno where r.enrolno='01096202722' and (r.schemeid = s.schemeid or paperid like '%'||progcode::text||'%') group by r.subcode, papername, paperid, passmarks, maxmarks, credits;
         * I really hope not to use this query more than once
         */
        var subjects = await (from r in _context.Results
            join s in _context.Subjects on r.Subcode equals s.Subcode
            join st in _context.Students on r.Enrolno equals st.Enrolno
            where r.Enrolno == enrollment && (r.Schemeid == s.Schemeid || s.Paperid.Contains(st.Progcode))
            group new { r, s, st } by new { s.Subcode, s.Paperid, s.Papername, s.Passmarks, s.Maxmarks, s.Credits }
            into g
            select new
            {
                g.Key.Subcode,
                g.Key.Paperid,
                g.Key.Papername,
                g.Key.Passmarks,
                g.Key.Maxmarks,
                g.Key.Credits
            }).ToListAsync();

        return subjects.Select(g => new Dictionary<string, string>
        {
            ["subcode"] = g.Subcode,
            ["paperid"] = g.Paperid,
            ["papername"] = g.Papername,
            ["passmarks"] = g.Passmarks.ToString() ?? "40",
            ["maxmarks"] = g.Maxmarks.ToString() ?? "100",
            ["credits"] = g.Credits.ToString() ?? "0"
        }).ToDictionary(g => g["subcode"], g => g);
    }
    
    private enum ExamType
    {
        [StringValue("REGULAR")]
        Regular,
        
        [StringValue("REVISED REGULAR")]
        RevisedRegular,
        
        [StringValue("RECHECKING REAPPEAR")]
        RecheckReappear,
        
        [StringValue("RECHECKING REGULAR")]
        RecheckRegular,
        
        [StringValue("REVISED REAPPEAR")]
        RevisedReappear,
        
        [StringValue("REAPPEAR")]
        Reappear,
        
        [StringValue("REVISED SUPPLEMENTARY")]
        RevisedSupplementary,
        
        [StringValue("SUPPLEMENTARY")]
        Supplementary,
    }
    
    public List<RankSenpaiSemester> GetRanklistBySemester(string instcode, string progcode, string batch, string sem)
    {
        Console.Out.WriteLine($"Instcode: {instcode}, Progcode: {progcode}, Batch: {batch}, Sem: {sem}");
        /*
         * select st.enrolno from results r inner join student st on st.enrolno=r.enrolno where st.enrolno like '___'||'962'||'027'||right('2022',2) and r.semester=1 group by st.enrolno;
         * select st.enrolno from results r inner join student st on st.enrolno=r.enrolno where st.instcode=962 and st.progcode='027' and st.batch=2022 and r.semester=1 group by st.enrolno;
         */

        // Fuck it, get the whole list
        // select r.enrolno, subcode, internal, external, total, semester, exam, resultdate from results r inner join student s on s.enrolno=r.enrolno where s.instcode=962 and s.progcode='027' and s.batch=2022 and r.semester=1 order by r.enrolno;

        // This does all of grouping on the db, but takes longer
        
        // var groupedResult = (from r in _context.Results
        // join s in _context.Students on r.Enrolno equals s.Enrolno
        // where s.Instcode.ToString() == instcode && s.Progcode == progcode && s.Batch.ToString() == batch && r.Semester.ToString() == sem
        // group r by r.Enrolno into g
        // orderby g.Key
        // select new
        // {
        //     Enrolno = g.Key,
        //     Name = g.Select(s => s.EnrolnoNavigation.Name).FirstOrDefault(),
        //     Subs = g.GroupBy(s => s.Subcode)
        //         .Select(subGroup => subGroup.OrderByDescending(s => s.Exam).First())
        //         .Select(s => new
        //     {
        //         Subcode = s.Subcode,
        //         Internal = s.Internal,
        //         External = s.External,
        //         Total = s.Total,
        //         Exam = s.Exam,
        //     }),
        //     Semester = g.Select(s => s.Semester).FirstOrDefault(),
        //     Resultdate = g.Select(s => s.Resultdate).FirstOrDefault()
        // }).ToList();
        
        var results = (from r in _context.Results
            join s in _context.Students on r.Enrolno equals s.Enrolno
            where s.Instcode.ToString() == instcode && s.Progcode == progcode && s.Batch.ToString() == batch && r.Semester.ToString() == sem
            orderby r.Enrolno
            select new
            {
                r.Enrolno,
                r.EnrolnoNavigation.Name,
                r.Subcode,
                r.Internal,
                r.External,
                r.Total,
                r.Semester,
                r.Exam,
                r.Resultdate
            }).ToList();
        
        // Group the data locally
        var groupedResult = results.GroupBy(g => g.Enrolno)
            .Select(g => new
            {
                Enrolno = g.Key,
                Name = g.Select(s => s.Name).FirstOrDefault(),
                Subs = g.GroupBy(s => s.Subcode)
                    .Select(subGroup => subGroup.OrderByDescending(s => s.Exam).First())
                    .Select(s => new
                {
                    Subcode = s.Subcode,
                    Internal = s.Internal,
                    External = s.External,
                    Total = s.Total,
                    Exam = s.Exam,
                }),
                Semester = g.Select(s => s.Semester).FirstOrDefault(),
                Resultdate = g.Select(s => s.Resultdate).FirstOrDefault()
            }).ToList();
        
        var subject = GetSubjectsByEnrollment(groupedResult[0].Enrolno).Result;

        // foreach (var i in subject)
        // {
        //     Console.Out.WriteLine($"{i.Key} => {i.Value}");
        // }
        
        // Console.Out.WriteLine(groupedResult[0]);
        // Console.Out.WriteLine(groupedResult[1]);
        
        List<RankSenpaiSemester> ranklist = new();
        foreach (var r in groupedResult)
        {
            RankSenpaiSemester rank = new()
            {
                Enrollment = r.Enrolno,
                Name = r.Name,
                Subject = new List<Dictionary<string, string>>()
            };
            int marks = 0;
            int total = 0;
            int creditmarks = 0;
            int totalcredits = 0;
            int totalcreditmarksweighted = 0;
            int totalcreditmarks = 0;
            foreach (var s in r.Subs)
            {
                marks += s.Total ?? 0;
                total += int.Parse(subject[s.Subcode]["maxmarks"]);
                creditmarks += int.Parse(subject[s.Subcode]["credits"]) * s.Total ?? 0;
                totalcreditmarksweighted += int.Parse(subject[s.Subcode]["credits"]) * MathSenpai.GetGradePoint(s.Total ?? 0);
                totalcreditmarks += int.Parse(subject[s.Subcode]["credits"]) * int.Parse(subject[s.Subcode]["maxmarks"]);
                totalcredits += int.Parse(subject[s.Subcode]["credits"]);
                rank.Subject.Add(new Dictionary<string, string>
                    {
                        ["subcode"] = s.Subcode,
                        ["subname"] = subject[s.Subcode]["papername"],
                        ["credits"] = subject[s.Subcode]["credits"],
                        ["paperid"] = subject[s.Subcode]["paperid"],
                        ["internal"] = s.Internal.ToString() ?? "0",
                        ["external"] = s.External.ToString() ?? "0",
                        ["total"] = s.Total.ToString() ?? "0",
                        ["exam"] = s.Exam,
                        ["grade"] = MathSenpai.GetGrade(s.Total ?? 0)
                    }
                );
            }
            rank.Marks = marks;
            rank.Total = total;
            rank.CreditMarks = creditmarks;
            rank.TotalCredits = totalcredits;
            rank.TotalCreditMarks = totalcreditmarks;
            rank.Percentage = (float)marks / total * 100;
            rank.CreditsPercentage = (float)creditmarks / totalcreditmarks * 100;
            rank.TotalCreditMarksWeighted = totalcreditmarksweighted;
            rank.Sgpa = MathSenpai.GetSgpa(totalcreditmarksweighted, totalcredits);
            
            ranklist.Add(rank);
        }

        return ranklist;
    }
    
    // public async Task<RankSenpaiSemester> GetRanklistOverall(string instcode, string progcode, string batch, string sem)
    // {
    //     Console.Out.WriteLine($"Instcode: {instcode}, Progcode: {progcode}, Batch: {batch}, Sem: {sem}");
    //
    //         var results = (from r in _context.Results
    //             join s in _context.Students on r.Enrolno equals s.Enrolno
    //             where s.Instcode.ToString() == instcode && s.Progcode == progcode && s.Batch.ToString() == batch && r.Semester.ToString() == sem
    //             orderby r.Enrolno
    //             select new
    //             {
    //                 r.Enrolno,
    //                 r.Subcode,
    //                 r.Internal,
    //                 r.External,
    //                 r.Total,
    //                 r.Semester,
    //                 r.Exam,
    //                 r.Resultdate
    //             }).ToList();
    //         // var subject = await subjects(results[0].Enrollment);
    //
    //         // await Console.Out.WriteLineAsync(subject.ToString());
    //         Console.Out.WriteLine(results[0]);
    //         // }
    //
    //     return new RankSenpaiSemester
    //     {
    //         Subject = new Dictionary<string, int>
    //             {
    //                 ["ES101"] = 99,
    //                 ["11111"] = 45
    //             }
    //     };
    //
    // }
}
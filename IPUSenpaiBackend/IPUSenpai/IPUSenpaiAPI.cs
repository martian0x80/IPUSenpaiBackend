using System.Globalization;
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
            .GroupBy(s => s.Batch)
            .OrderByDescending(s => s.Key)
            .Select(s => s.Key)
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
        _context.ChangeTracker.LazyLoadingEnabled = false;
        var semesters = await _context.Results.AsNoTracking()
            // .Include(r => r.EnrolnoNavigation)
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
        // var subjects = await (from r in _context.Results.AsNoTracking()
        //     join s in _context.Subjects on r.Subcode equals s.Subcode
        //     join st in _context.Students on r.Enrolno equals st.Enrolno
        //     where r.Enrolno == enrollment && (r.Schemeid == s.Schemeid || s.Paperid.Contains(st.Progcode))
        //     group new { r, s, st } by new { s.Subcode, s.Paperid, s.Papername, s.Passmarks, s.Maxmarks, s.Credits }
        //     into g
        //     select new
        //     {
        //         g.Key.Subcode,
        //         g.Key.Paperid,
        //         g.Key.Papername,
        //         g.Key.Passmarks,
        //         g.Key.Maxmarks,
        //         g.Key.Credits
        //     }).ToListAsync();
        _context.ChangeTracker.LazyLoadingEnabled = false;
        var subjects = await (from r in _context.Results.AsNoTracking()
            where r.Enrolno == enrollment
            join s in _context.Subjects on r.Subcode equals s.Subcode
            join st in _context.Students on r.Enrolno equals st.Enrolno
            where r.Schemeid == s.Schemeid || s.Paperid.Contains(st.Progcode)
            group new { r, s, st } by new { s.Subcode, s.Paperid, s.Papername, s.Passmarks, s.Maxmarks, s.Credits }
            into g
            select new
            {
                g.Key.Subcode,
                g.Key.Paperid,
                g.Key.Papername,
                g.Key.Passmarks,
                g.Key.Maxmarks,
                g.Key.Credits,
            }).ToListAsync();

        return subjects.Select(g => new Dictionary<string, string>
        {
            ["subcode"] = g.Subcode,
            ["paperid"] = g.Paperid,
            ["papername"] = g.Papername,
            ["passmarks"] = g.Passmarks.ToString() ?? "40",
            ["maxmarks"] = g.Maxmarks.ToString() ?? "100",
            ["credits"] = g.Credits.ToString() ?? "0"
        }).GroupBy(g => g["subcode"]).ToDictionary(g => g.Key, g => g.First());
        //    .ToDictionary(g => g["subcode"], g => g);
    }
    
    private enum ExamType
    {
        [StringValue("SUPPLEMENTARY")]
        Supplementary,
            
        [StringValue("REVISED SUPPLEMENTARY")]
        RevisedSupplementary,

        [StringValue("RECHECKING REAPPEAR")]
        RecheckReappear,

        [StringValue("REVISED REAPPEAR")]
        RevisedReappear,

        [StringValue("REAPPEAR")]
        Reappear,

        [StringValue("RECHECKING REGULAR")]
        RecheckRegular,

        [StringValue("REVISED REGULAR")]
        RevisedRegular,

        [StringValue("REGULAR")]
        Regular,
    }
    
    private ExamType GetExamType(string exam)
    {
        foreach (ExamType examType in Enum.GetValues(typeof(ExamType)))
        {
            if (exam.Contains(examType.StringValue()))
            {
                return examType;
            }
        }

        return ExamType.Regular;
    }

    public (List<RankSenpaiSemester>, int) GetRanklistBySemester(string instcode, string progcode, string batch, string sem,
        int pageNumber = 1, int pageSize = 10)
    {
        Console.Out.WriteLine($"Instcode: {instcode}, Progcode: {progcode}, Batch: {batch}, Sem: {sem}");
        /*
         * select st.enrolno from results r inner join student st on st.enrolno=r.enrolno where st.enrolno like '___'||'962'||'027'||right('2022',2) and r.semester=1 group by st.enrolno;
         * select st.enrolno from results r inner join student st on st.enrolno=r.enrolno where st.instcode=962 and st.progcode='027' and st.batch=2022 and r.semester=1 group by st.enrolno;
         */

        // Fuck it, get the whole list
        // select r.enrolno, subcode, internal, external, total, semester, exam, resultdate from results r inner join student s on s.enrolno=r.enrolno where s.instcode=962 and s.progcode='027' and s.batch=2022 and r.semester=1 order by r.enrolno;

        // This does all of grouping on the db, but takes too long
        /*
        // var groupedResult = (from r in _context.Results.AsNoTracking()
        //     where r.EnrolnoNavigation.Instcode.ToString() == instcode
        //           && r.EnrolnoNavigation.Progcode == progcode
        //           && r.EnrolnoNavigation.Batch.ToString() == batch
        //           && r.Semester.ToString() == sem
        //     orderby r.Enrolno
        //     group r by r.Enrolno into g
        //     select new
        //     {
        //         Enrolno = g.Key,
        //         Name = g.Select(s => s.EnrolnoNavigation.Name).FirstOrDefault(),
        //         Subs = g//.GroupBy(s => s.Subcode)
        //             //.Select(subGroup => subGroup.OrderByDescending(s => s.Exam).First())
        //             .Select(s => new
        //             {
        //                 Subcode = s.Subcode,
        //                 Internal = s.Internal,
        //                 External = s.External,
        //                 Total = s.Total,
        //                 Exam = s.Exam,
        //             }),
        //         Semester = g.Select(s => s.Semester).FirstOrDefault(),
        //         Resultdate = g.Select(s => s.Resultdate).FirstOrDefault()
        //     }).ToList();
        */
        _context.ChangeTracker.LazyLoadingEnabled = false;
        var results = (from r in _context.Results.AsNoTracking()
            where r.EnrolnoNavigation.Instcode.ToString() == instcode
                  && r.EnrolnoNavigation.Progcode == progcode
                  && r.EnrolnoNavigation.Batch.ToString() == batch
                  && r.Semester.ToString() == sem
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
                    .Select(subGroup => subGroup.OrderBy(s => GetExamType(s.Exam)).First())
                    .Select(s => new
                    {
                        Subcode = s.Subcode,
                        Internal = s.Internal,
                        External = s.External,
                        Total = s.Total,
                        Exam = s.Exam,
                        ExamType = GetExamType(s.Exam)
                    }),
                Semester = g.Select(s => s.Semester).FirstOrDefault(),
                Resultdate = g.Select(s => s.Resultdate).FirstOrDefault()
            }).ToList();
        
        if (groupedResult.Count == 0)
        {
            return (new List<RankSenpaiSemester>
            {
                new RankSenpaiSemester
                {
                    Enrollment = "No results found",
                    Name = "No results found",
                    Marks = 0,
                    Total = 0,
                    CreditMarks = 0,
                    TotalCredits = 0,
                    TotalCreditMarks = 0,
                    Percentage = 0,
                    CreditsPercentage = 0,
                    TotalCreditMarksWeighted = 0,
                    Sgpa = 0,
                    Subject = new List<Dictionary<string, string>>()
                }
            }, 0);
        }
        var subject = GetSubjectsByEnrollment(groupedResult[0].Enrolno).Result;

        List<RankSenpaiSemester> ranklist = new();
        object subjectLock = new();
        Parallel.ForEach(groupedResult, r =>
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

            Parallel.ForEach(r.Subs, s =>
            {
                if (!subject.ContainsKey(s.Subcode))
                {
                    lock (subjectLock)
                    {
                        if (!subject.ContainsKey(s.Subcode))
                        {
                            subject = GetSubjectsByEnrollment(r.Enrolno).Result;
                        }
                    }
                }

                marks += s.Total ?? 0;
                try
                {
                    total += int.Parse(subject[s.Subcode]["maxmarks"]);
                    creditmarks += int.Parse(subject[s.Subcode]["credits"]) * s.Total ?? 0;
                    totalcreditmarksweighted += int.Parse(subject[s.Subcode]["credits"]) *
                                                MathSenpai.GetGradePoint(s.Total ?? 0);
                    totalcreditmarks += int.Parse(subject[s.Subcode]["credits"]) *
                                        int.Parse(subject[s.Subcode]["maxmarks"]);
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
                            ["grade"] = MathSenpai.GetGrade(s.Total ?? 0),
                            ["ExamType"] = s.ExamType.StringValue()
                        }
                    );
                }
                catch (KeyNotFoundException e)
                {
                    // If key is not found retry
                    Console.Out.WriteLine($"Key not found: {s.Subcode}\n {r.Enrolno} {r.Name}\n Schemeid: {s.Exam}");

                }
            });

            rank.Marks = marks;
            rank.Total = total;
            rank.CreditMarks = creditmarks;
            rank.TotalCredits = totalcredits;
            rank.TotalCreditMarks = totalcreditmarks;
            if (total != 0)
            {
                rank.Percentage = (float)marks / total * 100;
            }
            else
            {
                rank.Percentage = 0;
            }
            if (totalcreditmarks != 0)
            {
                rank.CreditsPercentage = (float)creditmarks / totalcreditmarks * 100;
            }
            else
            {
                rank.CreditsPercentage = 0;
            }
            rank.TotalCreditMarksWeighted = totalcreditmarksweighted;
            rank.Sgpa = MathSenpai.GetSgpa(totalcreditmarksweighted, totalcredits);

            ranklist.Add(rank);
        });
        
        int count = ranklist.Count;

        ranklist =  ranklist.OrderByDescending(r => r.Sgpa).ThenBy(r => r.Marks)
            .Skip(pageNumber * pageSize).Take(pageSize).ToList();
        for (int i = 0; i < ranklist.Count; i++)
        {
            ranklist[i].Rank = i + 1;
        }
        return (ranklist, count);
    }

    public (List<RankSenpaiOverall>, int) GetRanklistOverall(string instcode, string progcode, string batch,
        int pageNumber = 1, int pageSize = 10)
    {
        Console.Out.WriteLine($"Instcode: {instcode}, Progcode: {progcode}, Batch: {batch}, Sem: Overall");

        _context.ChangeTracker.LazyLoadingEnabled = false;
        var results = (from r in _context.Results.AsNoTracking()
            where r.EnrolnoNavigation.Instcode.ToString() == instcode
                  && r.EnrolnoNavigation.Progcode == progcode
                  && r.EnrolnoNavigation.Batch.ToString() == batch
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
                Semester = g.GroupBy(s => s.Semester)
                    .Select(s => new
                    {
                        Semester = s.Key,
                        Subs = s.GroupBy(sub => sub.Subcode)
                            .Select(subGroup => subGroup.OrderBy(sub => GetExamType(sub.Exam)).First())
                            .Select(sub => new
                            {
                                Subcode = sub.Subcode,
                                Internal = sub.Internal,
                                External = sub.External,
                                Total = sub.Total,
                                Exam = sub.Exam,
                                ExamType = GetExamType(sub.Exam)
                            })
                    }).ToList()
            }).ToList();

        if (groupedResult.Count == 0)
        {
            return (new List<RankSenpaiOverall>
            {
                new RankSenpaiOverall
                {
                    Enrollment = "No results found",
                    Name = "No results found",
                    Marks = 0,
                    Total = 0,
                    CreditMarks = 0,
                    TotalCredits = 0,
                    TotalCreditMarks = 0,
                    Percentage = 0,
                    CreditsPercentage = 0,
                    TotalCreditMarksWeighted = 0,
                    Cgpa = 0,
                    SgpaAllSem = new List<Dictionary<string, string>>(),
                    MarksPerSemester = new List<Dictionary<string, int>>()
                }
            }, 0);
        }
        
        var subject = GetSubjectsByEnrollment(groupedResult[0].Enrolno).Result;

        List<RankSenpaiOverall> ranklist = new();
        object subjectLock = new();

        Parallel.ForEach(groupedResult, r =>
        {
            RankSenpaiOverall rank = new()
            {
                Enrollment = r.Enrolno,
                Name = r.Name,
                MarksPerSemester = new List<Dictionary<string, int>>(),
                SgpaAllSem = new List<Dictionary<string, string>>(),
                Semesters = r.Semester.Count
            };

            int marks = 0;
            int total = 0;
            int creditmarks = 0; // Total marks weighted by credits
            int totalcredits = 0; // Total credits
            int totalcreditmarksweighted = 0; // Total marks weighted by grade points
            int totalcreditmarks = 0; // Max marks
            float weightedsgpa = 0;

            Parallel.ForEach(r.Semester, s =>
            {
                int semestermarks = 0;
                int semestertotal = 0;
                int semestercreditmarks = 0;
                int semestercredits = 0;
                int semestercreditmarksweighted = 0;
                int semestercreditmarksmax = 0;
                Parallel.ForEach(s.Subs, sub =>
                {

                    if (!subject.ContainsKey(sub.Subcode))
                    {
                        lock (subjectLock)
                        {
                            if (!subject.ContainsKey(sub.Subcode))
                            {
                                subject = GetSubjectsByEnrollment(r.Enrolno).Result;
                            }
                        }
                    }

                    try
                    {
                        var maxmarks = int.Parse(subject[sub.Subcode]["maxmarks"]);
                        var credits = int.Parse(subject[sub.Subcode]["credits"]);
                        semestermarks += sub.Total ?? 0;
                        semestertotal += maxmarks;
                        semestercreditmarks += credits * sub.Total ?? 0;
                        semestercreditmarksweighted += credits *
                                                       MathSenpai.GetGradePoint(sub.Total ?? 0);
                        semestercreditmarksmax += credits * maxmarks;
                        semestercredits += credits;
                    }
                    catch (KeyNotFoundException e)
                    {
                        Console.Out.WriteLine($"Key not found: {sub.Subcode}\n {r.Enrolno} {r.Name}");
                    }
                });

                marks += semestermarks;
                total += semestertotal;
                creditmarks += semestercreditmarks;
                totalcreditmarksweighted += semestercreditmarksweighted;
                totalcreditmarks += semestercreditmarksmax;
                totalcredits += semestercredits;
                var sgpa = MathSenpai.GetSgpa(semestercreditmarksweighted, semestercredits);
                weightedsgpa += sgpa * semestercredits;
                rank.SgpaAllSem.Add(new Dictionary<string, string>
                {
                    ["semester"] = s.Semester.ToString(),
                    ["sgpa"] = sgpa.ToString(CultureInfo.InvariantCulture)
                });
                rank.MarksPerSemester.Add(new Dictionary<string, int>
                {
                    ["semester"] = s.Semester,
                    ["marks"] = semestermarks,
                    ["total"] = semestertotal,
                    ["creditmarks"] = semestercreditmarks,
                    ["totalcreditmarks"] = semestercreditmarksmax,
                    ["totalcredits"] = semestercredits,
                    ["totalcreditmarksweighted"] = semestercreditmarksweighted
                });
            });

            rank.Marks = marks;
            rank.Total = total;
            rank.CreditMarks = creditmarks;
            rank.TotalCredits = totalcredits;
            rank.TotalCreditMarks = totalcreditmarks;
            if (total != 0)
            {
                rank.Percentage = (float)marks / total * 100;
            }
            else
            {
                rank.Percentage = 0;
            }
            if (totalcreditmarks != 0)
            {
                rank.CreditsPercentage = (float)creditmarks / totalcreditmarks * 100;
            } else
            {
                rank.CreditsPercentage = 0;
            }
            rank.TotalCreditMarksWeighted = totalcreditmarksweighted;
            rank.Cgpa = MathSenpai.GetCgpa(weightedsgpa, totalcredits);

            ranklist.Add(rank);
        });
        
        int count = ranklist.Count;
        
        ranklist =  ranklist.OrderByDescending(r => r.Cgpa).ThenBy(r => r.Marks)
            .Skip(pageNumber * pageSize).Take(pageSize).ToList();
        for (int i = 0; i < ranklist.Count; i++)
        {
            ranklist[i].Rank = i + 1;
        }
        return (ranklist, count);
    }
    
}
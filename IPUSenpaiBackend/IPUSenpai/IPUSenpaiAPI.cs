using System.Collections.Concurrent;
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
        // Dependency Injections go brrr
        _logger = logger;
        _context = context;
    }

    // public async Task<StudentSenpai> GetStudentByEnrollment(string? enrollment)
    // {
    //     StudentSenpai student = await _context.Students
    //         .Include(s => s.InstcodeNavigation)
    //         .Include(s => s.ProgcodeNavigation)
    //         .Where(s => s.Enrolno == enrollment)
    //         .Select(s => new StudentSenpai
    //         {
    //             Enrolno = s.Enrolno,
    //             Name = s.Name,
    //             Instcode = s.Instcode,
    //             Institute = s.InstcodeNavigation.Instname,
    //             Progcode = s.Progcode,
    //             Programme = s.ProgcodeNavigation.Progname,
    //             Batch = s.Batch,
    //             Sid = s.Sid,
    //         }).FirstOrDefaultAsync();
    //
    //     if (student == null)
    //     {
    //         student = new StudentSenpai();
    //     }
    //
    //     return student;
    // }

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
        else
        {
            programmes.Insert(0, new PartialResponse
            {
                Name = "ALL INSTITUTES"
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

    public async Task<List<Response>> GetSpecializations(short limit = 30)
    {
        var specializations = await _context.Programmes
            .OrderBy(pi => pi.Spec)
            .Select(pi => new Response
            {
                Name = pi.Spec,
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

        var specializationsQuery = _context.ProgrammesInstitutes
            .Where(pi => pi.ProgcodeNavigation.Prog == prog);

        if (instname != "ALL INSTITUTES")
        {
            specializationsQuery = specializationsQuery.Where(pi => pi.InstcodeNavigation.Instname == instname);
        }

        var specializations = await specializationsQuery
            .OrderBy(pi => pi.ProgcodeNavigation.Spec)
            .Select(pi => new Response
            {
                Name = pi.ProgcodeNavigation.Spec,
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
        if (instname == "ALL INSTITUTES")
        {
            return new List<Response>
            {
                new Response
                {
                    Name = "All",
                    Value = "*"
                }
            };
        }

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

                shifts.Add(new Response
                {
                    Name = "All",
                    Value = "*"
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
        var batchesQuery = _context.Students
            .Where(s => s.ProgcodeNavigation.Prog == programme);
        if (institute != "ALL INSTITUTES")
        {
            batchesQuery = batchesQuery.Where(s => s.InstcodeNavigation.Instname == institute);
        }

        var batches = await batchesQuery
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
        return institute ?? new InstituteSenpai();
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
        return programme ?? new ProgrammeSenpai();
    }

    public async Task<List<PartialResponse>> GetSemestersByProgrammeInstnameBatch(string programme, string institute,
        string batch)
    {
        _context.ChangeTracker.LazyLoadingEnabled = false;
        var semestersQuery = _context.Results.AsNoTracking()
            // .Include(r => r.EnrolnoNavigation)
            .Where(r => r.EnrolnoNavigation.ProgcodeNavigation.Prog == programme &&
                        r.Batch.ToString() == batch);
        if (institute != "ALL INSTITUTES")
        {
            semestersQuery = semestersQuery.Where(r => r.EnrolnoNavigation.InstcodeNavigation.Instname == institute);
        }

        var semesters = await semestersQuery
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
        _logger.LogInformation($"Getting subjects for {enrollment}");
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
            // where s.Paperid.Contains(st.Progcode) || r.Schemeid == s.Schemeid
            where (s.Paperid.Contains(st.Progcode) ||
                   r.Schemeid == s.Schemeid) // || (!s.Paperid.Contains(st.Progcode) && r.Schemeid != s.Schemeid)
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
        [StringValue("SUPPLEMENTARY")] Supplementary,

        [StringValue("REVISED SUPPLEMENTARY")] RevisedSupplementary,

        [StringValue("RECHECKING REAPPEAR")] RecheckReappear,

        [StringValue("REVISED REAPPEAR")] RevisedReappear,

        [StringValue("REAPPEAR")] Reappear,

        [StringValue("RECHECKING REGULAR")] RecheckRegular,

        [StringValue("REVISED REGULAR")] RevisedRegular,

        [StringValue("REGULAR")] Regular,
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

    public (List<RankSenpaiSemester>, int, float, List<GpaListResponse>) GetRanklistBySemester(string instcode,
        string? instname,
        string progcode,
        string batch,
        string sem,
        int pageNumber = 0, int pageSize = 10)
    {
        _logger.LogInformation(
            $"\n[I] Grabbing semester ranklist for\nInstcode: {instcode}, Progcode: {progcode}, Batch: {batch}, Sem: {sem}, Instname: {instname}");
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

        // The last working version

        // var results = (from r in _context.Results.AsNoTracking()
        //     where r.EnrolnoNavigation.Instcode.ToString() == instcode
        //           && r.EnrolnoNavigation.Progcode == progcode
        //           && r.EnrolnoNavigation.Batch.ToString() == batch
        //           && r.Semester.ToString() == sem
        //     orderby r.Enrolno
        //     select new
        //     {
        //         r.Enrolno,
        //         r.EnrolnoNavigation.Name,
        //         r.Subcode,
        //         r.Internal,
        //         r.External,
        //         r.Total,
        //         r.Semester,
        //         r.Exam,
        //         r.Resultdate
        //     }).ToList();

        _context.ChangeTracker.LazyLoadingEnabled = false;
        var resultsQuery = _context.Results.AsNoTracking()
            // .Include(r => r.EnrolnoNavigation)
            .Where(r => r.EnrolnoNavigation.Progcode == progcode
                        && r.EnrolnoNavigation.Batch.ToString() == batch
                        && r.Semester.ToString() == sem);

        if (instcode == "*" && !string.IsNullOrEmpty(instname) && instname != "ALL INSTITUTES")
        {
            resultsQuery = resultsQuery.Where(r => r.EnrolnoNavigation.InstcodeNavigation.Instname == instname);
        }
        // else if (instcode == "*" && string.IsNullOrEmpty(instname))
        // {
        //     resultsQuery = resultsQuery.Where(r => r.EnrolnoNavigation.InstcodeNavigation.Instname == instname);
        // }
        else if (instcode != "*")
        {
            resultsQuery = resultsQuery.Where(r => r.EnrolnoNavigation.Instcode.ToString() == instcode);
        }

        var results = resultsQuery
            .OrderBy(r => r.Enrolno)
            .Select(r => new
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
            }, 0, 0, new List<GpaListResponse>());
        }

        var subject = GetSubjectsByEnrollment(groupedResult[0].Enrolno).Result;

        List<RankSenpaiSemester> ranklist = new();
        object subjectLock = new();
        short errorCount = 0;
        float AvgGpa = 0f;
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
                        if (!subject.ContainsKey(s.Subcode) && errorCount < 30)
                        {
                            subject = subject.Concat(GetSubjectsByEnrollment(r.Enrolno).Result)
                                .ToLookup(k => k.Key, v => v.Value)
                                .ToDictionary(k => k.Key, v => v.First());
                            // subject = GetSubjectsByEnrollment(r.Enrolno).Result;
                            errorCount++;
                        }
                        else if (errorCount >= 30)
                        {
                            Console.Out.WriteLine($"Key not found: {s.Subcode}\n {r.Enrolno} {r.Name}");
                            // Exit the loop
                            return;
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
                catch (KeyNotFoundException _)
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
            AvgGpa += rank.Sgpa / groupedResult.Count;
            ranklist.Add(rank);
        });

        int count = ranklist.Count;

        ranklist = ranklist.OrderByDescending(r => r.Sgpa).ThenByDescending(r => r.Marks).ToList();

        var gpaList = ranklist.Select(r => new GpaListResponse
        {
            Name = r.Name,
            Enrollment = r.Enrollment,
            Gpa = r.Sgpa,
            Percentage = r.Percentage
        }).ToList();

        if (errorCount >= 30)
        {
            ranklist.Insert(0, new RankSenpaiSemester
            {
                Enrollment = "6969696969",
                Name = "There might be issues with the data, missing subjects (~30) for some students",
                Marks = 69,
                Total = 69,
                CreditMarks = 69,
                TotalCredits = 69,
                TotalCreditMarks = 69,
                Percentage = 69,
                CreditsPercentage = 69,
                TotalCreditMarksWeighted = 69,
                Sgpa = 6.9f,
                Subject = new List<Dictionary<string, string>>()
            });
        }

        int i = 1;
        int rank = 1;
        ranklist[0].Rank = rank;
        while (i < ranklist.Count)
        {
            if (float.Abs(ranklist[i - 1].Sgpa - ranklist[i].Sgpa) < 0.0001f)
            {
                ranklist[i].Rank = rank;
            }
            else
            {
                ranklist[i].Rank = ++rank;
            }

            i++;
        }

        return (ranklist.Skip(pageNumber * pageSize).Take(pageSize).ToList(), count, AvgGpa, gpaList);
    }

    public (List<RankSenpaiOverall>, int, float, List<GpaListResponse>) GetRanklistOverall(string instcode,
        string? instname,
        string progcode,
        string batch,
        int pageNumber = 0, int pageSize = 10)
    {
        Console.Out.WriteLine(
            $"Instcode: {instcode}, Progcode: {progcode}, Batch: {batch}, Sem: Overall, Instname: {instname}");

        // Last working version

        // _context.ChangeTracker.LazyLoadingEnabled = false;
        // var results = (from r in _context.Results.AsNoTracking()
        //     where r.EnrolnoNavigation.Instcode.ToString() == instcode
        //           && r.EnrolnoNavigation.Progcode == progcode
        //           && r.EnrolnoNavigation.Batch.ToString() == batch
        //     orderby r.Enrolno
        //     select new
        //     {
        //         r.Enrolno,
        //         r.EnrolnoNavigation.Name,
        //         r.Subcode,
        //         r.Internal,
        //         r.External,
        //         r.Total,
        //         r.Semester,
        //         r.Exam,
        //         r.Resultdate
        //     }).ToList();

        _context.ChangeTracker.LazyLoadingEnabled = false;
        var resultsQuery = _context.Results.AsNoTracking()
            // .Include(r => r.EnrolnoNavigation)
            .Where(r => r.EnrolnoNavigation.Progcode == progcode
                        && r.EnrolnoNavigation.Batch.ToString() == batch);

        if (instcode == "*" && !string.IsNullOrEmpty(instname) && instname != "ALL INSTITUTES")
        {
            resultsQuery = resultsQuery.Where(r => r.EnrolnoNavigation.InstcodeNavigation.Instname == instname);
        }
        else if (instcode != "*")
        {
            resultsQuery = resultsQuery.Where(r => r.EnrolnoNavigation.Instcode.ToString() == instcode);
        }

        var results = resultsQuery
            .OrderBy(r => r.Enrolno)
            .Select(r => new
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
//                    SgpaAllSem = new List<Dictionary<string, string>>(),
                    MarksPerSemester = new List<Dictionary<string, string>>()
                }
            }, 0, 0, new List<GpaListResponse>());
        }

        var subject = GetSubjectsByEnrollment(groupedResult[0].Enrolno).Result;

        List<RankSenpaiOverall> ranklist = new();
        object subjectLock = new();
        short errorCount = 0;
        float AvgGpa = 0f;

        Parallel.ForEach(groupedResult, r =>
        {
            RankSenpaiOverall rank = new()
            {
                Enrollment = r.Enrolno,
                Name = r.Name,
                MarksPerSemester = new List<Dictionary<string, string>>(),
//                SgpaAllSem = new List<Dictionary<string, string>>(),
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
                            if (!subject.ContainsKey(sub.Subcode) && errorCount < 30)
                            {
                                subject = subject.Concat(GetSubjectsByEnrollment(r.Enrolno).Result)
                                    .ToLookup(k => k.Key, v => v.Value)
                                    .ToDictionary(k => k.Key, v => v.First());
                                // subject = GetSubjectsByEnrollment(r.Enrolno).Result;
                                errorCount++;
                            }
                            else if (errorCount >= 5)
                            {
                                Console.Out.WriteLine($"Key not found: {sub.Subcode}\n {r.Enrolno} {r.Name}");
                                // Exit the loop
                                return;
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
                // rank.SgpaAllSem.Add(new Dictionary<string, string>
                // {
                //     ["semester"] = s.Semester.ToString(),
                //     ["sgpa"] = sgpa.ToString(CultureInfo.InvariantCulture)
                // });
                rank.MarksPerSemester.Add(new Dictionary<string, string>
                {
                    ["semester"] = s.Semester.ToString(),
                    ["marks"] = semestermarks.ToString(),
                    ["total"] = semestertotal.ToString(),
                    ["creditmarks"] = semestercreditmarks.ToString(),
                    ["totalcreditmarks"] = semestercreditmarksmax.ToString(),
                    ["totalcredits"] = semestercredits.ToString(),
                    ["totalcreditmarksweighted"] = semestercreditmarksweighted.ToString(),
                    ["sgpa"] = sgpa.ToString(CultureInfo.InvariantCulture),
                    ["percentage"] = ((float)semestermarks / semestertotal * 100).ToString(),
                    ["creditspercentage"] = ((float)semestercreditmarks / semestercreditmarksmax * 100).ToString()
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
            }
            else
            {
                rank.CreditsPercentage = 0;
            }

            rank.TotalCreditMarksWeighted = totalcreditmarksweighted;
            rank.Cgpa = MathSenpai.GetCgpa(weightedsgpa, totalcredits);
            AvgGpa += rank.Cgpa / groupedResult.Count;
            ranklist.Add(rank);
        });

        int count = ranklist.Count;

        ranklist = ranklist.OrderByDescending(r => r.Cgpa).ThenByDescending(r => r.Marks).ToList();

        var gpaList = ranklist.Select(r => new GpaListResponse
        {
            Name = r.Name,
            Enrollment = r.Enrollment,
            Gpa = r.Cgpa,
            Percentage = r.Percentage
        }).ToList();

        if (errorCount >= 30)
        {
            ranklist.Insert(0, new RankSenpaiOverall
            {
                Enrollment = "6969696969",
                Name = "There might be issues with the data, missing subjects (~30) for some students",
                Marks = 69,
                Total = 69,
                CreditMarks = 69,
                TotalCredits = 69,
                TotalCreditMarks = 69,
                Percentage = 69,
                CreditsPercentage = 69,
                TotalCreditMarksWeighted = 69,
                Cgpa = 6.9f,
//                SgpaAllSem = new List<Dictionary<string, string>>(),
                MarksPerSemester = new List<Dictionary<string, string>>()
            });
        }

        int i = 1;
        int rank = 1;
        ranklist[0].Rank = rank;
        while (i < ranklist.Count)
        {
            if (float.Abs(ranklist[i - 1].Cgpa - ranklist[i].Cgpa) < 0.0001f)
            {
                ranklist[i].Rank = rank;
            }
            else
            {
                ranklist[i].Rank = ++rank;
            }

            i++;
        }

        return (ranklist.Skip(pageNumber * pageSize).Take(pageSize).ToList(), count, AvgGpa, gpaList);
    }

    // TODO: GetStudent aggregation support for upgradation/transfer students
    public StudentSenpai? GetStudent(string enrolno)
    {
        _logger.LogInformation($"\n [I] Getting student details for {enrolno}\n");

        var student = _context.Students
            .Where(s => s.Enrolno == enrolno)
            .Select(s => new
            {
                Enrolno = s.Enrolno,
                Name = s.Name,
                Instcode = s.Instcode,
                Institute = s.InstcodeNavigation.Instname,
                Progcode = s.Progcode,
                Programme = s.ProgcodeNavigation.Prog,
                Spec = s.ProgcodeNavigation.Spec,
                Batch = s.Batch,
                Sid = s.Sid,
            }).FirstOrDefault();

        if (student == null)
        {
            return null;
        }

        _context.ChangeTracker.LazyLoadingEnabled = false;
        var results = (from r in _context.Results.AsNoTracking()
            where r.EnrolnoNavigation.Enrolno == enrolno
            select new
            {
                r.Subcode,
                r.Internal,
                r.External,
                r.Total,
                r.Semester,
                r.Exam,
                r.Resultdate
            }).ToList();

        // Group the data locally
        var groupedResult = results.GroupBy(s => s.Semester)
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
            }).ToList();

        if (groupedResult.Count == 0)
        {
            return new StudentSenpai
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
            };
        }

        var subject = GetSubjectsByEnrollment(enrolno).Result;

        List<RankSenpaiSemester> ranklistSem = new();
        object subjectLock = new();
        short errorCount = 0;

        StudentSenpai studentSenpai = new()
        {
            Enrollment = enrolno,
            Name = student.Name,
            Institute = student.Institute,
            InstCode = student.Instcode.ToString(),
            Programme = student.Programme,
            Specialization = student.Spec,
            ProgCode = student.Progcode,
            Batch = student.Batch.ToString(),
            Sid = student.Sid,
            Marks = 0,
            CreditMarks = 0,
            TotalCreditMarks = 0,
            TotalCreditMarksWeighted = 0,
            TotalCredits = 0,
            Total = 0,
            Cgpa = 0,
            Percentage = 0,
            CreditsPercentage = 0,
            MarksPerSemester = new(),
            Subject = new(),
            CumulativeResult = new(),
            // CumulativePercentageBySem = new(),
            // CgpaByYear = new(),
            // CumulativePercentageByYear = new(),
            // SgpaAllSem = new()
            Semesters = groupedResult.Count,
        };

        int marks = 0;
        int total = 0;
        int creditmarks = 0; // Total marks weighted by credits
        int totalcredits = 0; // Total credits
        int totalcreditmarksweighted = 0; // Total marks weighted by grade points
        int totalcreditmarks = 0; // Max marks
        float weightedsgpa = 0;
        List<string> subs = new();

        foreach (var s in groupedResult)
        {
            int semestermarks = 0;
            int semestertotal = 0;
            int semestercreditmarks = 0;
            int semestercredits = 0;
            int semestercreditmarksweighted = 0;
            int semestercreditmarksmax = 0;

            studentSenpai.Subject.Add(new()
            {
                ["semester"] = s.Semester.ToString(),
                ["subjects"] = new List<Dictionary<string, string>>()
            });

            foreach (var sub in s.Subs)
            {
                if (!subject.ContainsKey(sub.Subcode))
                {
                    lock (subjectLock)
                    {
                        if (!subject.ContainsKey(sub.Subcode) && errorCount < 30)
                        {
                            subject = subject.Concat(GetSubjectsByEnrollment(enrolno).Result)
                                .ToLookup(k => k.Key, v => v.Value)
                                .ToDictionary(k => k.Key, v => v.First());
                            // subject = GetSubjectsByEnrollment(r.Enrolno).Result;
                            errorCount++;
                        }
                        // if (errorCount <= 30)
                        // {
                        //     Console.Out.WriteLine($"Key not found: {sub.Subcode}\n {enrolno} {student.Name}");
                        //     // Exit the loop
                        //     return;
                        // }
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

                    // Umm, race condition? I know this is a mess
                    try
                    {
                        ((List<Dictionary<string, string>>)studentSenpai.Subject.First(p =>
                                p["semester"].ToString() == s.Semester.ToString())["subjects"])
                            .Add(new Dictionary<string, string>
                            {
                                ["subcode"] = sub.Subcode,
                                ["subname"] = subject[sub.Subcode]["papername"],
                                ["credits"] = subject[sub.Subcode]["credits"],
                                ["paperid"] = subject[sub.Subcode]["paperid"],
                                ["internal"] = sub.Internal.ToString() ?? "0",
                                ["external"] = sub.External.ToString() ?? "0",
                                ["total"] = sub.Total.ToString() ?? "0",
                                ["exam"] = sub.Exam,
                                ["grade"] = MathSenpai.GetGrade(sub.Total ?? 0),
                                ["ExamType"] = sub.ExamType.StringValue()
                            });
                    }
                    catch (Exception e)
                    {
                        Console.Out.WriteLine($"ALERT!\nSemester not found: {s.Semester}\n {enrolno} {student.Name}");
                    }
                }
                catch (Exception e)
                {
                    Console.Out.WriteLine($"Key not found: {sub.Subcode}\n {enrolno} {student.Name}");
                }
            }

            marks += semestermarks;
            total += semestertotal;
            creditmarks += semestercreditmarks;
            totalcreditmarksweighted += semestercreditmarksweighted;
            totalcreditmarks += semestercreditmarksmax;
            totalcredits += semestercredits;
            var sgpa = MathSenpai.GetSgpa(semestercreditmarksweighted, semestercredits);
            weightedsgpa += sgpa * semestercredits;
            studentSenpai.MarksPerSemester.Add(new()
            {
                ["semester"] = s.Semester.ToString(),
                ["marks"] = semestermarks.ToString(),
                ["total"] = semestertotal.ToString(),
                ["creditmarks"] = semestercreditmarks.ToString(),
                ["totalcreditmarks"] = semestercreditmarksmax.ToString(),
                ["totalcredits"] = semestercredits.ToString(),
                ["totalcreditmarksweighted"] = semestercreditmarksweighted.ToString(),
                ["sgpa"] = sgpa.ToString(CultureInfo.InvariantCulture),
                ["percentage"] = ((float)semestermarks / semestertotal * 100).ToString(CultureInfo.InvariantCulture),
                ["creditspercentage"] =
                    ((float)semestercreditmarks / semestercreditmarksmax * 100).ToString(CultureInfo.InvariantCulture)
            });
        }

        /*
         *  _MarksPerSemester
           _Subject
           _CgpaBySem
           _CumulativePercentageBySem
           _CgpaByYear
           _CumulativePercentageByYear
           _SgpaAllSem
         */

        // I know this seems like a mess, but it's the best I could do, the json serializer didn't like the concurrentbag
        // Don't judge me for my atrocities
        //
        // studentSenpai.MarksPerSemester = _MarksPerSemester.Select(dict => new Dictionary<string, int>(dict)).ToList();
        // studentSenpai.Subject = _Subject.Select(dict => new Dictionary<string, object>(dict)).ToList();
        // studentSenpai.CgpaBySem = _CgpaBySem.Select(dict => new Dictionary<string, string>(dict)).ToList();
        // studentSenpai.CumulativePercentageBySem = _CumulativePercentageBySem.Select(dict => new Dictionary<string, string>(dict)).ToList();
        // studentSenpai.CgpaByYear = _CgpaByYear.Select(dict => new Dictionary<string, string>(dict)).ToList();
        // studentSenpai.CumulativePercentageByYear = _CumulativePercentageByYear.Select(dict => new Dictionary<string, string>(dict)).ToList();
        // studentSenpai.SgpaAllSem = _SgpaAllSem.Select(dict => new Dictionary<string, string>(dict)).ToList();
        //

        // Nvm, fuck concurrency

        studentSenpai.Marks = marks;
        studentSenpai.Total = total;
        studentSenpai.CreditMarks = creditmarks;
        studentSenpai.TotalCredits = totalcredits;
        studentSenpai.TotalCreditMarks = totalcreditmarks;
        if (total != 0)
        {
            studentSenpai.Percentage = (float)marks / total * 100;
        }
        else
        {
            studentSenpai.Percentage = 0;
        }

        if (totalcreditmarks != 0)
        {
            studentSenpai.CreditsPercentage = (float)creditmarks / totalcreditmarks * 100;
        }
        else
        {
            studentSenpai.CreditsPercentage = 0;
        }

        studentSenpai.TotalCreditMarksWeighted = totalcreditmarksweighted;
        studentSenpai.Cgpa = MathSenpai.GetCgpa(weightedsgpa, totalcredits);

        var sems = studentSenpai.MarksPerSemester.Select(s => int.Parse(s["semester"])).OrderBy(s => s).ToList();
        var weightedSgpa = 0f;
        var totalCredits = 0;
        var cmarks = 0;
        var ctotal = 0;
        var creditMarks = 0;
        var totalCreditMarks = 0;
        var sgpaCovered = new List<int>();
        foreach (var sem in sems)
        {
            sgpaCovered.Add(sem);
            var _sem = studentSenpai.MarksPerSemester.First(s => int.Parse(s["semester"]) == sem);
            var sgpa = float.Parse(_sem["sgpa"]);
            var credits = int.Parse(_sem["totalcredits"]);
            weightedSgpa += sgpa * credits;
            totalCredits += credits;
            // studentSenpai.CgpaBySem.Add(new()
            // {
            //     ["semester"] = sgpaCovered.Select(s => s.ToString()).Aggregate((s1, s2) => s1 + "+" + s2),
            //     ["cgpa"] = MathSenpai.GetCgpa(weightedSgpa, totalCredits).ToString(CultureInfo.InvariantCulture)
            // });
            var _marks = int.Parse(_sem["marks"]);
            var _creditMarks = int.Parse(_sem["creditmarks"]);
            var _total = int.Parse(_sem["total"]);
            var _totalCreditMarks = int.Parse(_sem["totalcreditmarks"]);

            cmarks += _marks;
            ctotal += _total;
            creditMarks += _creditMarks;
            totalCreditMarks += _totalCreditMarks;

            studentSenpai.CumulativeResult.Add(new()
            {
                ["semester"] = sgpaCovered.Select(s => s.ToString()).Aggregate((s1, s2) => s1 + "+" + s2),
                ["cgpa"] = MathSenpai.GetCgpa(weightedSgpa, totalCredits).ToString(CultureInfo.InvariantCulture),
                ["percentage"] = ((float)cmarks / ctotal * 100).ToString(CultureInfo.InvariantCulture),
                ["creditspercentage"] =
                    ((float)creditMarks / totalCreditMarks * 100).ToString(CultureInfo.InvariantCulture),
                ["marks"] = cmarks.ToString(),
                ["totalmarks"] = ctotal.ToString(),
                ["creditmarks"] = creditMarks.ToString(),
                ["totalcreditmarks"] = totalCreditMarks.ToString()
            });
        }
        // for (int i = 0; i < sems.Count; i++)
        // {
        //     var _sems = sems.Take(i + 1).ToList();
        //     var _weightedSgpa = 0f;
        //     var _totalCredits = 0;
        //     foreach (var sem in _sems)
        //     {
        //         _weightedSgpa += float.Parse(studentSenpai.SgpaAllSem.First(s => int.Parse(s["semester"]) == sem)["sgpa"]) *
        //                          studentSenpai.MarksPerSemester.First(s => s["semester"] == sem)["totalcredits"];
        //         _totalCredits += studentSenpai.MarksPerSemester.First(s => s["semester"] == sem)["totalcredits"];
        //     }
        //     studentSenpai.CgpaByYear.Add(new()
        //     {
        //         ["semester"] = _sems.Select(s => s.ToString()).Aggregate((s1, s2) => s1 + "+" + s2),
        //         ["cgpa"] = MathSenpai.GetCgpa(_weightedSgpa, _totalCredits).ToString(CultureInfo.InvariantCulture)
        //     });
        //     var _marks = _sems.Select(s => studentSenpai.MarksPerSemester.First(s => s["semester"].ToString() == s.ToString())["marks"]).Sum();
        //     var _creditMarks = _sems.Select(s => studentSenpai.MarksPerSemester.First(s => s["semester"].ToString() == s.ToString())["creditmarks"]).Sum();
        //     var _total = _sems.Select(s => studentSenpai.MarksPerSemester.First(s => s["semester"].ToString() == s.ToString())["total"]).Sum();
        //     var _totaCreditMarks = _sems.Select(s => studentSenpai.MarksPerSemester.First(s => s["semester"].ToString() == s.ToString())["totalcreditmarks"]).Sum();
        //     studentSenpai.CumulativePercentageByYear.Add(new()
        //     {
        //         ["semester"] = _sems.Select(s => s.ToString()).Aggregate((s1, s2) => s1 + "+" + s2),
        //         ["percentage"] = (float)_marks / _total * 100 + "%",
        //         ["creditspercentage"] = (float)_creditMarks / _totaCreditMarks * 100 + "%"
        //     });
        // }

        return studentSenpai;
    }

    public async Task<List<StudentSearchSenpai>> SearchStudent(StudentSearchFilterOptionsSenpai? filter)
    {
        if (filter != null)
        {
            var students = _context.Students.Where(s => s.Name.Contains(filter.Name));

            if (!string.IsNullOrEmpty(filter.Institute))
            {
                students = students.Where(s => s.InstcodeNavigation.Instname == filter.Institute);
            }

            if (!string.IsNullOrEmpty(filter.Programme))
            {
                students = students.Where(s => s.ProgcodeNavigation.Prog == filter.Programme);
            }

            if (!string.IsNullOrEmpty(filter.Batch))
            {
                students = students.Where(s => s.Batch.ToString() == filter.Batch);
            }

            return await students.Select(s => new StudentSearchSenpai
            {
                Enrollment = s.Enrolno,
                Name = s.Name,
                Institute = s.InstcodeNavigation.Instname,
                Programme = s.ProgcodeNavigation.Prog,
                Batch = s.Batch.ToString(),
            }).ToListAsync();
        }

        return new List<StudentSearchSenpai>();
    }
}
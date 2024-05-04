using System.Globalization;
// using Microsoft.EntityFrameworkCore;
using System.Reflection;
using Dapper;
using IPUSenpaiBackend.CustomEntities;
using IPUSenpaiBackend.DBContext;

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

// ReSharper disable once InconsistentNaming
public class IPUSenpaiAPI : IIPUSenpaiAPI
{
    private readonly IDapperContext _context;
    private readonly ILogger _logger;

    public IPUSenpaiAPI(IDapperContext context, ILogger<IPUSenpaiAPI> logger)
    {
        // Dependency Injections go brrr
        _logger = logger;
        _context = context;
        _logger.LogInformation("IPUSenpaiAPI created");

        // // Test Dapper
        // using (var connection = _context.CreateConnection())
        // {
        //     var result = connection.Query("SELECT * FROM institute");
        //     foreach (var row in result)
        //     {
        //         Console.WriteLine(row);
        //     }
        // }
    }

    public async Task<List<Response>> GetInstitutes(short limit = 100)
    {
        var query =
            @"SELECT i.instname AS Name, (
            SELECT i1.instcode
            FROM student AS s0
            LEFT JOIN institute AS i1 ON s0.instcode = i1.instcode
            WHERE i.instname = i1.instname
            LIMIT 1)::text AS Value
        FROM student AS s
        LEFT JOIN institute AS i ON s.instcode = i.instcode
        GROUP BY i.instname
            HAVING count(*)::int > 100
        ORDER BY count(*)::int DESC;";

        using (var connection = _context.CreateConnection())
        {
            var institutes = await connection.QueryAsync<Response>(query, new { Limit = limit });
            return institutes.ToList();
        }

        // var institutes = await _context.Students
        //     .Include(s => s.InstcodeNavigation)
        //     .GroupBy(s => s.InstcodeNavigation.Instname)
        //     .Where(s => s.Count() > 100)
        //     .OrderByDescending(s => s.Count())
        //     .Select(s => new Response
        //     {
        //         Name = s.Key,
        //         Value = s.FirstOrDefault().InstcodeNavigation.Instcode.ToString()
        //     })
        //     .ToListAsync();
        // Total 107 unique institutes
        // var institutes = (from s in _context.Institutes
        //     join i in _context.Students on s.Instcode equals i.Instcode
        //     group s by s.Instname into g
        //     // where g.Count() > 500
        //     orderby g.Count() descending
        //     select g.Key).Take(limit);
    }

    public async Task<List<PartialResponse>> GetInstitutesByProgramme(
        string programme,
        short limit = 100
    )
    {
        // var programmes = await _context.ProgrammesInstitutes
        //     .Include(pi => pi.InstcodeNavigation)
        //     .Where(pi => pi.ProgcodeNavigation.Prog == programme)
        //     .GroupBy(pi => pi.InstcodeNavigation.Instname)
        //     .OrderBy(pi => pi.Key)
        //     .Select(pi => new PartialResponse
        //     {
        //         Name = pi.Key
        //     })
        //     // .OrderBy(instname => instname.Name)
        //     .ToListAsync();
        // student ?
        var query =
            @"SELECT i.instname AS Name
              FROM student AS p
              LEFT JOIN programme AS p0 ON p.progcode = p0.progcode
              LEFT JOIN institute AS i ON p.instcode = i.instcode
              WHERE p0.prog=@Programme
              GROUP BY i.instname
              ORDER BY i.instname";
        List<PartialResponse> programmes = [new() { Name = "ALL INSTITUTES" },];

        using (var connection = _context.CreateConnection())
        {
            programmes.AddRange(
                await connection.QueryAsync<PartialResponse>(
                    query,
                    new { Programme = programme, Limit = limit }
                )
            );
        }

        if (programmes.Count == 1)
        {
            programmes = new() { new PartialResponse { Name = "No institutes found", } };
        }

        return programmes;
    }

    public async Task<List<PartialResponse>> GetProgrammes(short limit = 79)
    {
        // var programmes = await _context.Programmes
        //     .GroupBy(p => p.Prog)
        //     .OrderBy(p => p.Key)
        //     .Select(p => new PartialResponse
        //     {
        //         Name = p.Key
        //     })
        //     // .OrderBy(p => p.Name)
        //     .Take(limit)
        //     .ToListAsync();

        var query =
            @"SELECT p.prog AS Name
              FROM programme AS p
              GROUP BY p.prog
              ORDER BY p.prog
              LIMIT @Limit";

        List<PartialResponse> programmes;
        using (var connection = _context.CreateConnection())
        {
            programmes = (
                await connection.QueryAsync<PartialResponse>(query, new { Limit = limit })
            ).ToList();
        }

        if (programmes.Count == 0)
        {
            programmes.Add(new PartialResponse { Name = "No programmes found", });
        }

        return programmes;
    }

    public async Task<List<Response>> GetSpecializations(short limit = 30)
    {
        // var specializations = await _context.Programmes
        //     .OrderBy(pi => pi.Spec)
        //     .Select(pi => new Response
        //     {
        //         Name = pi.Spec,
        //         Value = pi.Progcode
        //     })
        //     .Distinct()
        //     .ToListAsync();

        var query =
            @"SELECT DISTINCT p.spec AS Name, p.progcode AS Value
                FROM programme AS p";
        List<Response> specializations;

        using (var connection = _context.CreateConnection())
        {
            specializations = (await connection.QueryAsync<Response>(query)).ToList();
        }

        if (specializations.Count == 0)
        {
            return new List<Response>
            {
                new() { Name = "No specializations found", Value = "No specializations found" }
            };
        }

        return specializations;
    }

    public async Task<List<Response>> GetSpecializationsByProgrammeAndInstname(
        short limit = 30,
        string? prog = "BACHELOR OF TECHNOLOGY",
        string? instname = "University School of Information & Communication Technology"
    )
    {
        var builder = new SqlBuilder();
        var selector = builder.AddTemplate(
            @"SELECT DISTINCT p0.spec AS Name, p.progcode AS Value
                FROM programmes_institutes AS p
                /**leftjoin**/
                /**where**/
                /**orderby**/"
        );

        builder.LeftJoin("programme AS p0 ON p.progcode = p0.progcode");
        builder.Where("p0.prog = @Programme", new { Programme = prog });
        if (instname != "ALL INSTITUTES")
        {
            builder.LeftJoin("institute AS i ON p.instcode = i.instcode");
            builder.Where("i.instname = @Instname", new { Instname = instname });
        }

        builder.OrderBy("p0.spec");

        using (var connection = _context.CreateConnection())
        {
            var specializations = (
                await connection.QueryAsync<Response>(selector.RawSql, selector.Parameters)
            ).ToList();
            if (specializations.Count == 0)
            {
                return new List<Response>
                {
                    new() { Name = "No specializations found", Value = "No specializations found" }
                };
            }

            return specializations;
        }

        // var specializationsQuery = _context.ProgrammesInstitutes
        //     .Where(pi => pi.ProgcodeNavigation.Prog == prog);
        //
        // if (instname != "ALL INSTITUTES")
        // {
        //     specializationsQuery = specializationsQuery.Where(pi => pi.InstcodeNavigation.Instname == instname);
        // }
        //
        // var specializations = await specializationsQuery
        //     .OrderBy(pi => pi.ProgcodeNavigation.Spec)
        //     .Select(pi => new Response
        //     {
        //         Name = pi.ProgcodeNavigation.Spec,
        //         Value = pi.Progcode
        //     })
        //     .Distinct()
        //     .ToListAsync();
    }

    public async Task<List<Response>> GetInstituteCodesForShifts(string instname)
    {
        if (instname == "ALL INSTITUTES")
        {
            return new List<Response>
            {
                new() { Name = "All", Value = "*" }
            };
        }

        // var institutes = await _context.Institutes
        //     .Where(i => i.Instname == instname)
        //     .Select(i => i.Instcode)
        //     .OrderBy(s => s)
        //     .ToListAsync();

        var query =
            @"SELECT i.instcode
              FROM institute AS i
              WHERE i.instname = @Instname
              ORDER BY i.instcode";
        List<short> institutes;

        using (var connection = _context.CreateConnection())
        {
            institutes = (
                await connection.QueryAsync<short>(query, new { Instname = instname })
            ).ToList();
        }

        List<Response> shifts = new();

        /*
         * Assign shifts to each institute code, the smallest institute code gets the morning shift
         * and the next smallest gets the evening shift
         * and the last one gets the other shift
         */
        if (institutes.Count > 0)
        {
            shifts.Add(new Response { Name = "Morning", Value = institutes[0].ToString() });
            if (institutes.Count > 1)
            {
                shifts.Add(new Response { Name = "Evening", Value = institutes[1].ToString() });

                shifts.Add(new Response { Name = "All", Value = "*" });
            }

            if (institutes.Count > 2)
            {
                shifts.Add(new Response { Name = "Other", Value = institutes[2].ToString() });
            }
        }

        return shifts;
    }

    public async Task<List<short?>> GetBatchesByPrognameAndInstname(
        string programme,
        string institute
    )
    {
        // var batchesQuery = _context.Students
        //     .Where(s => s.ProgcodeNavigation.Prog == programme);
        // if (institute != "ALL INSTITUTES")
        // {
        //     batchesQuery = batchesQuery.Where(s => s.InstcodeNavigation.Instname == institute);
        // }
        // var batches = await batchesQuery
        //     .GroupBy(s => s.Batch)
        //     .OrderByDescending(s => s.Key)
        //     .Select(s => s.Key)
        //     .ToListAsync();

        var builder = new SqlBuilder();
        var selector = builder.AddTemplate(
            @"SELECT s.batch
                  FROM student AS s
                    /**leftjoin**/
                    /**where**/
                  GROUP BY s.batch
                  ORDER BY s.batch DESC"
        );
        builder.LeftJoin("programme AS p ON s.progcode = p.progcode");
        builder.Where("p.prog = CAST(@Programme AS TEXT)", new { Programme = programme });

        if (institute != "ALL INSTITUTES")
        {
            builder.LeftJoin("institute AS i ON s.instcode = i.instcode");
            builder.Where("i.instname = CAST(@Instname AS TEXT)", new { Instname = institute });
        }

        using (var connection = _context.CreateConnection())
        {
            var batches = (
                await connection.QueryAsync<short?>(selector.RawSql, selector.Parameters)
            ).ToList();
            return batches;
        }
    }

    public async Task<InstituteSenpai> GetInstituteByInstcode(short? instcode)
    {
        // var institute = await _context.Institutes
        //     .Where(i => i.Instcode == instcode)
        //     .Select(i => new InstituteSenpai
        //     {
        //         Instcode = i.Instcode,
        //         Instname = i.Instname,
        //     })
        //     .Take(1)
        //     .FirstOrDefaultAsync();

        var query =
            @"SELECT i.instcode AS Instcode, i.instname AS Instname
              FROM institute AS i
              WHERE i.instcode = CAST(@Instcode AS SMALLINT)
              LIMIT 1";

        using (var connection = _context.CreateConnection())
        {
            var institute = await connection.QueryFirstOrDefaultAsync<InstituteSenpai>(
                query,
                new { Instcode = instcode }
            );
            return institute ?? new InstituteSenpai();
        }
    }

    public async Task<ProgrammeSenpai> GetProgrammeByProgcode(string? progcode)
    {
        // var programme = await _context.Programmes
        //     .Where(p => p.Progcode == progcode)
        //     .Select(p => new ProgrammeSenpai
        //     {
        //         Progcode = p.Progcode,
        //         Progname = p.Progname,
        //         Prog = p.Prog,
        //         Spec = p.Spec
        //     })
        //     .Take(1)
        //     .FirstOrDefaultAsync();

        var query =
            @"SELECT p.progcode AS Progcode, p.progname AS Progname, p.prog AS Prog, p.spec AS Spec
              FROM programme AS p
              WHERE p.progcode = CAST(@Progcode AS VARCHAR(8))
              LIMIT 1";
        using (var connection = _context.CreateConnection())
        {
            var programme = await connection.QueryFirstOrDefaultAsync<ProgrammeSenpai>(
                query,
                new { Progcode = progcode }
            );
            return programme ?? new ProgrammeSenpai();
        }
    }

    public async Task<List<PartialResponse>> GetSemestersByProgrammeInstnameBatch(
        string programme,
        string institute,
        string batch
    )
    {
        //     var semestersQuery = Results.AsNoTracking()
        //         // .Include(r => r.EnrolnoNavigation)
        //         .Where(r => r.EnrolnoNavigation.ProgcodeNavigation.Prog == programme &&
        //                     r.Batch.ToString() == batch);
        //     if (institute != "ALL INSTITUTES")
        //     {
        //         semestersQuery = semestersQuery.Where(r => r.EnrolnoNavigation.InstcodeNavigation.Instname == institute);
        //     }
        //
        //     var semesters = await semestersQuery
        //         .GroupBy(r => r.Semester)
        //         .Select(r => new PartialResponse
        //         {
        //             Name = r.Key.ToString()
        //         })
        //         .OrderBy(r => r.Name)
        //         .ToListAsync();

        var builder = new SqlBuilder();
        var selector = builder.AddTemplate(
            @"SELECT r.semester::text AS Name
                FROM results AS r
                /**innerjoin**/
                /**leftjoin**/
                /**where**/
                GROUP BY r.semester
                ORDER BY r.semester::text"
        );

        builder.Where("p.prog = CAST(@Programme AS TEXT)", new { Programme = programme });
        builder.Where("r.batch = CAST(@Batch AS SMALLINT)", new { Batch = batch });
        builder.InnerJoin("student AS s ON r.enrolno = s.enrolno");
        builder.LeftJoin("programme AS p ON s.progcode = p.progcode");

        if (institute != "ALL INSTITUTES")
        {
            builder.LeftJoin("institute AS i ON s.instcode = i.instcode");
            builder.Where("i.instname = CAST(@Instname AS TEXT)", new { Instname = institute });
        }

        using (var connection = _context.CreateConnection())
        {
            var semesters = (
                await connection.QueryAsync<PartialResponse>(selector.RawSql, selector.Parameters)
            ).ToList();
            return semesters;
        }
    }

    public async Task<Dictionary<string, Dictionary<string, string>>> GetSubjectsBySID(
        string? Sid,
        string? Enrollment,
        bool failover = false
    )
    {
        _logger.LogInformation($"Getting subjects for SID: {Sid}");
        var query =
            @"SELECT s.subcode AS Subcode, s.paperid AS Paperid, s.papername AS Papername, s.passmarks AS Passmarks, s.maxmarks AS Maxmarks, s.credits AS Credits
              FROM results AS r
              INNER JOIN subjects AS s ON r.subcode = s.subcode
              INNER JOIN student AS s0 ON r.enrolno = s0.enrolno
              WHERE s0.sid = @Sid AND ((s.paperid IS NOT NULL AND s0.progcode IS NOT NULL AND strpos(s.paperid, s0.progcode) > 0) OR r.schemeid = s.schemeid)
              GROUP BY s.subcode, s.paperid, s.papername, s.passmarks, s.maxmarks, s.credits";

        if (failover)
        {
            query =
                @"SELECT s.subcode AS Subcode, s.paperid AS Paperid, s.papername AS Papername, s.passmarks AS Passmarks, s.maxmarks AS Maxmarks, s.credits AS Credits
              FROM results AS r
              INNER JOIN subjects AS s ON r.subcode = s.subcode
              INNER JOIN student AS s0 ON r.enrolno = s0.enrolno
              WHERE r.enrolno = @Enrollment AND (r.subcode = s.subcode) OR (r.subcode = s.paperid)
              GROUP BY s.subcode, s.paperid, s.papername, s.passmarks, s.maxmarks, s.credits";
        }

        using (var connection = _context.CreateConnection())
        {
            var subjects = (
                await connection.QueryAsync<SubjectSenpai>(query, new { Sid = Sid, Enrollment = Enrollment })
            ).ToList();
            return subjects
                .Select(g => new Dictionary<string, string>
                {
                    ["subcode"] = g.Subcode ?? "N/A",
                    ["paperid"] = g.Paperid ?? "N/A",
                    ["papername"] = g.Papername ?? "N/A",
                    ["passmarks"] = g.Passmarks.ToString() ?? "40",
                    ["maxmarks"] = g.Maxmarks.ToString() ?? "100",
                    ["credits"] = g.Credits.ToString() ?? "0"
                })
                .GroupBy(g => g["subcode"])
                .ToDictionary(g => g.Key, g => g.First());
            //    .ToDictionary(g => g["subcode"], g => g);
        }
    }

    public async Task<Dictionary<string, Dictionary<string, string>>> GetSubjectsByEnrollment(
        string? enrollment,
        bool failover = false
    )
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

        // This one akchuly works!!

        // var subjects = await (from r in _context.Results.AsNoTracking()
        //     where r.Enrolno == enrollment
        //     join s in _context.Subjects on r.Subcode equals s.Subcode
        //     join st in _context.Students on r.Enrolno equals st.Enrolno
        //     // where s.Paperid.Contains(st.Progcode) || r.Schemeid == s.Schemeid
        //     where (s.Paperid.Contains(st.Progcode) ||
        //            r.Schemeid == s.Schemeid) // || (!s.Paperid.Contains(st.Progcode) && r.Schemeid != s.Schemeid)
        //     group new { r, s, st } by new { s.Subcode, s.Paperid, s.Papername, s.Passmarks, s.Maxmarks, s.Credits }
        //     into g
        //     select new
        //     {
        //         g.Key.Subcode,
        //         g.Key.Paperid,
        //         g.Key.Papername,
        //         g.Key.Passmarks,
        //         g.Key.Maxmarks,
        //         g.Key.Credits,
        //     }).ToListAsync();

        var query =
            @"SELECT s.subcode AS Subcode, s.paperid AS Paperid, s.papername AS Papername, s.passmarks AS Passmarks, s.maxmarks AS Maxmarks, s.credits AS Credits
              FROM results AS r
              INNER JOIN subjects AS s ON r.subcode = s.subcode
              INNER JOIN student AS s0 ON r.enrolno = s0.enrolno
              WHERE r.enrolno = @Enrollment AND ((s.paperid IS NOT NULL AND s0.progcode IS NOT NULL AND strpos(s.paperid, s0.progcode) > 0) OR r.schemeid = s.schemeid)
              GROUP BY s.subcode, s.paperid, s.papername, s.passmarks, s.maxmarks, s.credits";

        if (failover)
        {
            query =
                @"SELECT s.subcode AS Subcode, s.paperid AS Paperid, s.papername AS Papername, s.passmarks AS Passmarks, s.maxmarks AS Maxmarks, s.credits AS Credits
              FROM results AS r
              INNER JOIN subjects AS s ON r.subcode = s.subcode
              INNER JOIN student AS s0 ON r.enrolno = s0.enrolno
              WHERE r.enrolno = @Enrollment AND ((r.subcode = s.subcode) OR (r.subcode = s.paperid))
              GROUP BY s.subcode, s.paperid, s.papername, s.passmarks, s.maxmarks, s.credits";
        }

        using (var connection = _context.CreateConnection())
        {
            var subjects = (
                await connection.QueryAsync<SubjectSenpai>(query, new { Enrollment = enrollment })
            ).ToList();

            return subjects
                .Select(g => new Dictionary<string, string>
                {
                    ["subcode"] = g.Subcode ?? "N/A",
                    ["paperid"] = g.Paperid ?? "N/A",
                    ["papername"] = g.Papername ?? "N/A",
                    ["passmarks"] = g.Passmarks.ToString() ?? "40",
                    ["maxmarks"] = g.Maxmarks.ToString() ?? "100",
                    ["credits"] = g.Credits.ToString() ?? "0"
                })
                .GroupBy(g => g["subcode"])
                .ToDictionary(g => g.Key, g => g.First());
            //    .ToDictionary(g => g["subcode"], g => g);
        }
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

    private class Result
    {
        public string Enrolno { get; set; }
        public string Name { get; set; }
        public string Subcode { get; set; }
        public int? Internal { get; set; }
        public int? External { get; set; }
        public int? Total { get; set; }
        public string Semester { get; set; }
        public string Exam { get; set; }
        public string Resultdate { get; set; }
    }

    public (
        List<RankSenpaiSemester>,
        int,
        float,
        float,
        List<GpaListResponse>
        ) GetRanklistBySemester(
            string instcode,
            string? instname,
            string progcode,
            string batch,
            string sem,
            int pageNumber = 0,
            int pageSize = 10
        )
    {
        _logger.LogInformation(
            $"\n[I] Grabbing semester ranklist for\nInstcode: {instcode}, Progcode: {progcode}, Batch: {batch}, Sem: {sem}, Instname: {instname}"
        );
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

        // var resultsQuery = _context.Results.AsNoTracking()
        //     // .Include(r => r.EnrolnoNavigation)
        //     .Where(r => r.EnrolnoNavigation.Progcode == progcode
        //                 && r.EnrolnoNavigation.Batch.ToString() == batch
        //                 && r.Semester.ToString() == sem);
        //
        // if (instcode == "*" && !string.IsNullOrEmpty(instname) && instname != "ALL INSTITUTES")
        // {
        //     resultsQuery = resultsQuery.Where(r => r.EnrolnoNavigation.InstcodeNavigation.Instname == instname);
        // }
        // // else if (instcode == "*" && string.IsNullOrEmpty(instname))
        // // {
        // //     resultsQuery = resultsQuery.Where(r => r.EnrolnoNavigation.InstcodeNavigation.Instname == instname);
        // // }
        // else if (instcode != "*")
        // {
        //     resultsQuery = resultsQuery.Where(r => r.EnrolnoNavigation.Instcode.ToString() == instcode);
        // }
        //
        // var results = resultsQuery
        //     .OrderBy(r => r.Enrolno)
        //     .Select(r => new
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

        var builder = new SqlBuilder();
        var selector = builder.AddTemplate(
            @"SELECT r.enrolno AS Enrolno, s.name AS Name, r.subcode AS Subcode, r.internal AS Internal, r.external AS External, r.total AS Total, r.semester AS Semester, r.exam AS Exam, r.resultdate AS Resultdate
            FROM results AS r
            /**innerjoin**/
            /**leftjoin**/
            /**where**/
            ORDER BY r.enrolno"
        );

        builder.Where("s.progcode = CAST(@Progcode AS VARCHAR(12))", new { Progcode = progcode });
        builder.Where("s.batch = CAST(@Batch AS SMALLINT)", new { Batch = batch });
        builder.Where("r.semester = CAST(@Sem AS SMALLINT)", new { Sem = sem });
        builder.InnerJoin("student AS s ON r.enrolno = s.enrolno");

        if (instcode == "*" && !string.IsNullOrEmpty(instname) && instname != "ALL INSTITUTES")
        {
            builder.LeftJoin("institute AS i ON s.instcode = i.instcode");
            builder.Where("i.instname = CAST(@Instname AS TEXT)", new { Instname = instname });
        }
        else if (instcode != "*")
        {
            builder.Where("s.instcode = CAST(@Instcode AS SMALLINT)", new { Instcode = instcode });
        }

        List<Result> results;

        using (var connection = _context.CreateConnection())
        {
            results = (connection.Query<Result>(selector.RawSql, selector.Parameters)).ToList();
        }

        // Group the data locally
        var groupedResult = results
            .GroupBy(g => g.Enrolno)
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
            })
            .ToList();

        if (groupedResult.Count == 0)
        {
            return (
                new List<RankSenpaiSemester>
                {
                    new()
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
                },
                0,
                0,
                0,
                new List<GpaListResponse>()
            );
        }

        var subject = GetSubjectsByEnrollment(groupedResult[0].Enrolno).Result;

        if (subject.Count == 0)
        {
            subject = GetSubjectsByEnrollment(groupedResult[0].Enrolno, true).Result;
        }

        List<RankSenpaiSemester> ranklist = new();
        object subjectLock = new();
        short errorCount = 0;
        float AvgGpa = 0f;
        float AvgPercentage = 0f;

        Parallel.ForEach(
            groupedResult,
            r =>
            {
                RankSenpaiSemester rank =
                    new()
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
                    if (!subject.ContainsKey(s.Subcode))
                    {
                        lock (subjectLock)
                        {
                            if (!subject.ContainsKey(s.Subcode) && errorCount < 30)
                            {
                                subject = subject
                                    .Concat(GetSubjectsByEnrollment(r.Enrolno).Result)
                                    .ToLookup(k => k.Key, v => v.Value)
                                    .ToDictionary(k => k.Key, v => v.First());
                                if (subject.Count == 0)
                                {
                                    subject = subject
                                        .Concat(GetSubjectsByEnrollment(r.Enrolno, true).Result)
                                        .ToLookup(k => k.Key, v => v.Value)
                                        .ToDictionary(k => k.Key, v => v.First());
                                }

                                // subject = GetSubjectsByEnrollment(r.Enrolno).Result;
                                errorCount++;
                            }
                            else if (errorCount >= 30)
                            {
                                Console.Out.WriteLine(
                                    $"Key not found: {s.Subcode}\n {r.Enrolno} {r.Name}"
                                );
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
                        totalcreditmarksweighted +=
                            int.Parse(subject[s.Subcode]["credits"])
                            * MathSenpai.GetGradePoint(s.Total ?? 0);
                        totalcreditmarks +=
                            int.Parse(subject[s.Subcode]["credits"])
                            * int.Parse(subject[s.Subcode]["maxmarks"]);
                        totalcredits += int.Parse(subject[s.Subcode]["credits"]);
                        rank.Subject.Add(
                            new Dictionary<string, string>
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
                        Console.Out.WriteLine(
                            $"Key not found: {s.Subcode}\n {r.Enrolno} {r.Name}\n Schemeid: {s.Exam}"
                        );
                    }
                }

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
                AvgPercentage += rank.Percentage / groupedResult.Count;
                ranklist.Add(rank);
            }
        );

        int count = ranklist.Count;

        ranklist = ranklist
            .OrderByDescending(r => r.Sgpa)
            .ThenByDescending(r => r.Marks)
            .ThenByDescending(r => r.CreditMarks)
            .ToList();

        var gpaList = ranklist
            .Select(r => new GpaListResponse
            {
                Name = r.Name,
                Enrollment = r.Enrollment,
                Gpa = r.Sgpa,
                Percentage = r.Percentage
            })
            .ToList();

        if (errorCount >= 30)
        {
            ranklist.Insert(
                0,
                new RankSenpaiSemester
                {
                    Enrollment = "6969696969",
                    Name =
                        "There might be issues with the data, missing subjects (~30) for some students",
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
                }
            );
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

        return (
            ranklist.Skip(pageNumber * pageSize).Take(pageSize).ToList(),
            count,
            AvgGpa,
            AvgPercentage,
            gpaList
        );
    }

    public (List<RankSenpaiOverall>, int, float, float, List<GpaListResponse>) GetRanklistOverall(
        string instcode,
        string? instname,
        string progcode,
        string batch,
        int pageNumber = 0,
        int pageSize = 10
    )
    {
        Console.Out.WriteLine(
            $"Instcode: {instcode}, Progcode: {progcode}, Batch: {batch}, Sem: Overall, Instname: {instname}"
        );

        // var resultsQuery = _context.Results.AsNoTracking()
        //     // .Include(r => r.EnrolnoNavigation)
        //     .Where(r => r.EnrolnoNavigation.Progcode == progcode
        //                 && r.EnrolnoNavigation.Batch.ToString() == batch);
        //
        // if (instcode == "*" && !string.IsNullOrEmpty(instname) && instname != "ALL INSTITUTES")
        // {
        //     resultsQuery = resultsQuery.Where(r => r.EnrolnoNavigation.InstcodeNavigation.Instname == instname);
        // }
        // else if (instcode != "*")
        // {
        //     resultsQuery = resultsQuery.Where(r => r.EnrolnoNavigation.Instcode.ToString() == instcode);
        // }
        //
        // var results = resultsQuery
        //     .OrderBy(r => r.Enrolno)
        //     .Select(r => new
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

        var builder = new SqlBuilder();
        var selector = builder.AddTemplate(
            @"SELECT r.enrolno AS Enrolno, s.name AS Name, r.subcode AS Subcode, r.internal AS Internal, r.external AS External, r.total AS Total, r.semester AS Semester, r.exam AS Exam, r.resultdate AS Resultdate
            FROM results AS r
            /**innerjoin**/
            /**leftjoin**/
            /**where**/
            ORDER BY r.enrolno"
        );

        builder.Where("s.progcode = CAST(@Progcode AS VARCHAR(12))", new { Progcode = progcode });
        builder.Where("s.batch = CAST(@Batch AS SMALLINT)", new { Batch = batch });
        builder.InnerJoin("student AS s ON r.enrolno = s.enrolno");

        if (instcode == "*" && !string.IsNullOrEmpty(instname) && instname != "ALL INSTITUTES")
        {
            builder.LeftJoin("institute AS i ON s.instcode = i.instcode");
            builder.Where("i.instname = CAST(@Instname AS TEXT)", new { Instname = instname });
        }
        else if (instcode != "*")
        {
            builder.Where("s.instcode = CAST(@Instcode AS SMALLINT)", new { Instcode = instcode });
        }

        List<Result> results;

        using (var connection = _context.CreateConnection())
        {
            results = (connection.Query<Result>(selector.RawSql, selector.Parameters)).ToList();
        }

        // Group the data locally
        var groupedResult = results
            .GroupBy(g => g.Enrolno)
            .Select(g => new
            {
                Enrolno = g.Key,
                Name = g.Select(s => s.Name).FirstOrDefault(),
                Semester = g.GroupBy(s => s.Semester)
                    .Select(s => new
                    {
                        Semester = s.Key,
                        Subs = s.GroupBy(sub => sub.Subcode)
                            .Select(subGroup =>
                                subGroup.OrderBy(sub => GetExamType(sub.Exam)).First()
                            )
                            .Select(sub => new
                            {
                                Subcode = sub.Subcode,
                                Internal = sub.Internal,
                                External = sub.External,
                                Total = sub.Total,
                                Exam = sub.Exam,
                                ExamType = GetExamType(sub.Exam)
                            })
                    })
                    .ToList()
            })
            .ToList();

        if (groupedResult.Count == 0)
        {
            return (
                new List<RankSenpaiOverall>
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
                },
                0,
                0,
                0,
                new List<GpaListResponse>()
            );
        }

        var subject = GetSubjectsByEnrollment(groupedResult[0].Enrolno).Result;
        if (subject.Count == 0)
        {
            subject = GetSubjectsByEnrollment(groupedResult[0].Enrolno, true).Result;
        }

        List<RankSenpaiOverall> ranklist = new();
        object subjectLock = new();
        short errorCount = 0;
        float AvgGpa = 0f;
        float AvgPercentage = 0f;

        Parallel.ForEach(
            groupedResult,
            r =>
            {
                RankSenpaiOverall rank =
                    new()
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

                Parallel.ForEach(
                    r.Semester,
                    s =>
                    {
                        int semestermarks = 0;
                        int semestertotal = 0;
                        int semestercreditmarks = 0;
                        int semestercredits = 0;
                        int semestercreditmarksweighted = 0;
                        int semestercreditmarksmax = 0;
                        foreach (var sub in s.Subs)
                        {
                            if (!subject.ContainsKey(sub.Subcode))
                            {
                                lock (subjectLock)
                                {
                                    if (!subject.ContainsKey(sub.Subcode) && errorCount < 30)
                                    {
                                        subject = subject
                                            .Concat(GetSubjectsByEnrollment(r.Enrolno).Result)
                                            .ToLookup(k => k.Key, v => v.Value)
                                            .ToDictionary(k => k.Key, v => v.First());
                                        if (subject.Count == 0)
                                        {
                                            subject = subject
                                                .Concat(
                                                    GetSubjectsByEnrollment(r.Enrolno, true).Result
                                                )
                                                .ToLookup(k => k.Key, v => v.Value)
                                                .ToDictionary(k => k.Key, v => v.First());
                                        }

                                        // subject = GetSubjectsByEnrollment(r.Enrolno).Result;
                                        errorCount++;
                                    }
                                    else if (errorCount >= 5)
                                    {
                                        Console.Out.WriteLine(
                                            $"Key not found: {sub.Subcode}\n {r.Enrolno} {r.Name}"
                                        );
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
                                semestercreditmarksweighted +=
                                    credits * MathSenpai.GetGradePoint(sub.Total ?? 0);
                                semestercreditmarksmax += credits * maxmarks;
                                semestercredits += credits;
                            }
                            catch (KeyNotFoundException e)
                            {
                                Console.Out.WriteLine(
                                    $"Key not found: {sub.Subcode}\n {r.Enrolno} {r.Name}"
                                );
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
                        // rank.SgpaAllSem.Add(new Dictionary<string, string>
                        // {
                        //     ["semester"] = s.Semester.ToString(),
                        //     ["sgpa"] = sgpa.ToString(CultureInfo.InvariantCulture)
                        // });
                        rank.MarksPerSemester.Add(
                            new Dictionary<string, string>
                            {
                                ["semester"] = s.Semester.ToString(),
                                ["marks"] = semestermarks.ToString(),
                                ["total"] = semestertotal.ToString(),
                                ["creditmarks"] = semestercreditmarks.ToString(),
                                ["totalcreditmarks"] = semestercreditmarksmax.ToString(),
                                ["totalcredits"] = semestercredits.ToString(),
                                ["totalcreditmarksweighted"] =
                                    semestercreditmarksweighted.ToString(),
                                ["sgpa"] = sgpa.ToString(CultureInfo.InvariantCulture),
                                ["percentage"] = (
                                    (float)semestermarks / semestertotal * 100
                                ).ToString(CultureInfo.InvariantCulture),
                                ["creditspercentage"] = (
                                    (float)semestercreditmarks / semestercreditmarksmax * 100
                                ).ToString(CultureInfo.InvariantCulture)
                            }
                        );
                    }
                );

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
                AvgPercentage += rank.Percentage / groupedResult.Count;
                ranklist.Add(rank);
            }
        );

        int count = ranklist.Count;

        ranklist = ranklist.OrderByDescending(r => r.Cgpa).ThenByDescending(r => r.Marks).ToList();

        var gpaList = ranklist
            .Select(r => new GpaListResponse
            {
                Name = r.Name,
                Enrollment = r.Enrollment,
                Gpa = r.Cgpa,
                Percentage = r.Percentage
            })
            .ToList();

        if (errorCount >= 30)
        {
            ranklist.Insert(
                0,
                new RankSenpaiOverall
                {
                    Enrollment = "6969696969",
                    Name =
                        "There might be issues with the data, missing subjects (~30) for some students",
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
                }
            );
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

        return (
            ranklist.Skip(pageNumber * pageSize).Take(pageSize).ToList(),
            count,
            AvgGpa,
            AvgPercentage,
            gpaList
        );
    }

    private class Student
    {
        public string? Enrolno { get; set; }
        public string? Name { get; set; }
        public string? Instcode { get; set; }
        public string? Institute { get; set; }
        public string? Progcode { get; set; }
        public string? Programme { get; set; }
        public string? Spec { get; set; }
        public string? Batch { get; set; }
        public string? Sid { get; set; }
    }

    public StudentSenpai? GetStudent(string enrolno)
    {
        _logger.LogInformation($"\n [I] Getting student details for {enrolno}\n");

        // var student = _context.Students
        //     .Where(s => s.Enrolno == enrolno)
        //     .Select(s => new
        //     {
        //         Enrolno = s.Enrolno,
        //         Name = s.Name,
        //         Instcode = s.Instcode,
        //         Institute = s.InstcodeNavigation.Instname,
        //         Progcode = s.Progcode,
        //         Programme = s.ProgcodeNavigation.Prog,
        //         Spec = s.ProgcodeNavigation.Spec,
        //         Batch = s.Batch,
        //         Sid = s.Sid,
        //     }).FirstOrDefault();
        //
        // if (student == null)
        // {
        //     return null;
        // }
        //
        // var results = (from r in _context.Results.AsNoTracking()
        //     where r.EnrolnoNavigation.Enrolno == enrolno
        //     select new
        //     {
        //         r.Subcode,
        //         r.Internal,
        //         r.External,
        //         r.Total,
        //         r.Semester,
        //         r.Exam,
        //         r.Resultdate
        //     }).ToList();

        bool transfer = false;

        var query0 =
            @"SELECT s.enrolno AS Enrolno, s.name AS Name, s.instcode AS Instcode, i.instname AS Institute, s.progcode AS Progcode, p.prog AS Programme, p.spec AS Spec, s.batch AS Batch, s.sid AS Sid
                FROM student AS s
                INNER JOIN institute AS i ON s.instcode = i.instcode
                INNER JOIN programme AS p ON s.progcode = p.progcode
                WHERE s.enrolno = CAST(@Enrollment AS VARCHAR(12))";

        var query1 = "SELECT count(*) FROM student WHERE sid = CAST(@Sid AS VARCHAR(20))";

        var query2 = new SqlBuilder();
        var selector = query2.AddTemplate(
            @"SELECT r.subcode AS Subcode, r.internal AS Internal, r.external AS External, r.total AS Total, r.semester AS Semester, r.exam AS Exam, r.resultdate AS Resultdate
              FROM results AS r
                /**innerjoin**/
                /**where**/
              ORDER BY r.enrolno"
        );

        List<Result> results;
        Student? student;
        using (var connection = _context.CreateConnection())
        {
            student = connection
                .Query<Student>(query0, new { Enrollment = enrolno })
                .FirstOrDefault();
            if (student == null)
            {
                return null;
            }

            var sidCount = connection
                .Query<int>(query1, new { Sid = student.Sid })
                .FirstOrDefault();
            if (sidCount > 1)
            {
                query2.InnerJoin("student AS s ON r.enrolno = s.enrolno");
                query2.Where("s.sid = CAST(@Sid AS VARCHAR(20))", new { Sid = student.Sid });
                results = (connection.Query<Result>(selector.RawSql, selector.Parameters)).ToList();
                transfer = true;
                _logger.LogInformation(
                    $"[I] Student {enrolno} ({student.Sid}) has transferred from another institute"
                );
            }
            else
            {
                query2.Where(
                    "r.enrolno = CAST(@Enrolno AS VARCHAR(12))",
                    new { Enrolno = enrolno }
                );
                results = (connection.Query<Result>(selector.RawSql, selector.Parameters)).ToList();
            }
        }

        // Group the data locally
        var groupedResult = results
            .GroupBy(s => s.Semester)
            .Select(s => new
            {
                Semester = s.Key,
                Subs = s.GroupBy(sub => sub.Subcode)
                    .Select(subGroup => subGroup.OrderBy(sub => GetExamType(sub.Exam)).First())
                    .Select(sub => new
                    {
                        Subcode = sub.Subcode,
                        Name = sub.Name,
                        Internal = sub.Internal,
                        External = sub.External,
                        Total = sub.Total,
                        Exam = sub.Exam,
                        ExamType = GetExamType(sub.Exam)
                    })
            })
            .ToList();

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

        var subject = transfer
            ? GetSubjectsBySID(student.Sid, enrolno).Result
            : GetSubjectsByEnrollment(enrolno).Result;

        _logger.LogInformation($"[I] Subjects found for {enrolno} {student.Name}: {subject.Count}");

        // foreach (var sub in subject)
        // {
        //     Console.Out.WriteLine(
        //         $"{sub.Key} {sub.Value["papername"]} {sub.Value["paperid"]} {sub.Value["subcode"]}"
        //     );
        // }

        if (subject.Count == 0)
        {
            _logger.LogInformation($"[I] No subjects found for {enrolno} {student.Name}");
            _logger.LogInformation($"[I] Failover query initiated for {enrolno} {student.Name}");
            subject = transfer
                ? GetSubjectsBySID(student.Sid, enrolno, true).Result
                : GetSubjectsByEnrollment(enrolno, true).Result;
        }

        // Find duplicate subjects, if any and remove them
        // foreach (var s in subject)
        // {
        //     if (subject.Count(x => x.Value["Subname"] == s.Value["Subname"]) > 1)
        //     {
        //         if (s.Value == subject.First(x => x.Value["Subname"] == s.Value["Subname"]).Value)
        //         {
        //             subject[""] = s.Value;
        //         }
        //     }
        // }


        List<RankSenpaiSemester> ranklistSem = new();
        object subjectLock = new();
        short errorCount = 0;

        StudentSenpai studentSenpai =
            new()
            {
                Enrollment = enrolno,
                Name = student.Name,
                Institute = student.Institute,
                InstCode = student.Instcode,
                Programme = student.Programme,
                Specialization = student.Spec,
                ProgCode = student.Progcode,
                Batch = student.Batch,
                Sid = student.Sid,
                Transfer = transfer,
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

            studentSenpai.Subject.Add(
                new()
                {
                    ["semester"] = s.Semester.ToString(),
                    ["subjects"] = new List<Dictionary<string, string>>()
                }
            );

            foreach (var sub in s.Subs)
            {
                if (!subject.ContainsKey(sub.Subcode))
                {
                    lock (subjectLock)
                    {
                        if (!subject.ContainsKey(sub.Subcode) && errorCount < 30)
                        {
                            subject = subject
                                .Concat(GetSubjectsByEnrollment(enrolno).Result)
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
                    semestercreditmarksweighted +=
                        credits * MathSenpai.GetGradePoint(sub.Total ?? 0);
                    semestercreditmarksmax += credits * maxmarks;
                    semestercredits += credits;

                    try
                    {
                        (
                            (List<Dictionary<string, string>>)
                            studentSenpai.Subject.First(p =>
                                p["semester"].ToString() == s.Semester
                            )["subjects"]
                        ).Add(
                            new Dictionary<string, string>
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
                            }
                        );
                    }
                    catch (Exception e)
                    {
                        Console.Out.WriteLine(
                            $"ALERT!\nSemester not found: {s.Semester}\n {enrolno} {student.Name}"
                        );
                    }
                }
                catch (Exception e)
                {
                    Console.Out.WriteLine(
                        $"Key not found: {sub.Subcode}\n {enrolno} {student.Name}"
                    );
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
            studentSenpai.MarksPerSemester.Add(
                new()
                {
                    ["semester"] = s.Semester.ToString(),
                    ["marks"] = semestermarks.ToString(),
                    ["total"] = semestertotal.ToString(),
                    ["creditmarks"] = semestercreditmarks.ToString(),
                    ["totalcreditmarks"] = semestercreditmarksmax.ToString(),
                    ["totalcredits"] = semestercredits.ToString(),
                    ["totalcreditmarksweighted"] = semestercreditmarksweighted.ToString(),
                    ["sgpa"] = sgpa.ToString(CultureInfo.InvariantCulture),
                    ["percentage"] = ((float)semestermarks / semestertotal * 100).ToString(
                        CultureInfo.InvariantCulture
                    ),
                    ["creditspercentage"] = (
                        (float)semestercreditmarks / semestercreditmarksmax * 100
                    ).ToString(CultureInfo.InvariantCulture)
                }
            );
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

        var sems = studentSenpai
            .MarksPerSemester.Select(s => int.Parse(s["semester"]))
            .OrderBy(s => s)
            .ToList();
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

            studentSenpai.CumulativeResult.Add(
                new()
                {
                    ["semester"] = sgpaCovered
                        .Select(s => s.ToString())
                        .Aggregate((s1, s2) => s1 + "+" + s2),
                    ["cgpa"] = MathSenpai
                        .GetCgpa(weightedSgpa, totalCredits)
                        .ToString(CultureInfo.InvariantCulture),
                    ["percentage"] = ((float)cmarks / ctotal * 100).ToString(
                        CultureInfo.InvariantCulture
                    ),
                    ["creditspercentage"] = ((float)creditMarks / totalCreditMarks * 100).ToString(
                        CultureInfo.InvariantCulture
                    ),
                    ["marks"] = cmarks.ToString(),
                    ["totalmarks"] = ctotal.ToString(),
                    ["creditmarks"] = creditMarks.ToString(),
                    ["totalcreditmarks"] = totalCreditMarks.ToString()
                }
            );
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

    public async Task<List<StudentSearchSenpai>> SearchStudent(
        StudentSearchFilterOptionsSenpai? filter
    )
    {
        if (filter != null)
        {
            // var students = _context.Students.Where(s => s.Name.Contains(filter.Name));
            var builder = new SqlBuilder();
            var selector = builder.AddTemplate(
                @"SELECT s.enrolno AS Enrollment, s.name AS Name, i.instname AS Institute, p.prog AS Programme, s.batch AS Batch
                    FROM student AS s
                    INNER JOIN institute AS i ON s.instcode = i.instcode
                    INNER JOIN programme AS p ON s.progcode = p.progcode
                    /**where**/
                    ORDER BY s.enrolno"
            );
            builder.Where("s.name ILIKE CAST(@Name AS TEXT)", new { Name = $"%{filter.Name}%" });

            if (!string.IsNullOrEmpty(filter.Institute))
            {
                // students = students.Where(s => s.InstcodeNavigation.Instname == filter.Institute);
                builder.Where(
                    "i.instname = CAST(@Institute AS TEXT)",
                    new { Institute = filter.Institute }
                );
            }

            if (!string.IsNullOrEmpty(filter.Programme))
            {
                // students = students.Where(s => s.ProgcodeNavigation.Prog == filter.Programme);
                builder.Where(
                    "p.prog = CAST(@Programme AS TEXT)",
                    new { Programme = filter.Programme }
                );
            }

            if (!string.IsNullOrEmpty(filter.Batch))
            {
                // students = students.Where(s => s.Batch.ToString() == filter.Batch);
                builder.Where("s.batch = CAST(@Batch AS SMALLINT)", new { Batch = filter.Batch });
            }

            // await students.Select(s => new StudentSearchSenpai
            // {
            //     Enrollment = s.Enrolno,
            //     Name = s.Name,
            //     Institute = s.InstcodeNavigation.Instname,
            //     Programme = s.ProgcodeNavigation.Prog,
            //     Batch = s.Batch.ToString(),
            // }).ToListAsync();

            using (var connection = _context.CreateConnection())
            {
                return (
                    await connection.QueryAsync<StudentSearchSenpai>(
                        selector.RawSql,
                        selector.Parameters
                    )
                ).ToList();
            }
        }

        return new List<StudentSearchSenpai>();
    }
}
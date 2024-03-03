using IPUSenpaiBackend.CustomEntities;
using IPUSenpaiBackend.DBContext;
using Microsoft.EntityFrameworkCore;

namespace IPUSenpaiBackend.IPUSenpai;
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
    
    public async Task<List<string?>> GetInstitutes(short limit = 30)
    {
        /*
         * select instname, count(*) from institute inner join student on student.instcode=institute.instcode group by institute.instname having count(*) > 100 order by count(*) desc;
         */
        // var institutes = await _context.Students
        //     .Include(s => s.InstcodeNavigation)
        //     .GroupBy(s => s.InstcodeNavigation.Instname)
        //     .Where(s => s.Count() > 100)
        //     .OrderByDescending(s => s.Count())
        //     .Select(s => s.Key)
        //     .ToListAsync();
        // Total 107 unique institutes
        var institutes = (from s in _context.Institutes
            join i in _context.Students on s.Instcode equals i.Instcode
            group s by s.Instname into g
            // where g.Count() > 500
            orderby g.Count() descending
            select g.Key).Take(limit);
        return await institutes.ToListAsync();
    }

    public async Task<List<string?>> GetProgrammes(short limit = 30)
    {
        var programmes = await _context.Programmes
            .GroupBy(p => p.Prog)
            .OrderByDescending(p => p.Count())
            .Select(p => p.Key)
            .Take(limit)
            .ToListAsync();
        if (programmes.Count == 0)
        {
            programmes.Add("No programmes found");
        }
        return programmes;
    }
    
    public async Task<List<string?>> GetSpecializations(short limit = 30, string? prog = "")
    {
        var specializations = await _context.Programmes
            .Where(p => p.Prog == prog)
            .Select(p => p.Spec)
            .Take(limit)
            .ToListAsync();
        if (specializations.Count == 0)
        {
            specializations.Add("No specializations found");
        }
        return specializations;
    }
    
    // public async Task<List<StudentSenpai>> GetStudentsByInstitute(string? institute)
    // {
    //     List<StudentSenpai> students = await _context.Students
    //         .Include(s => s.InstcodeNavigation)
    //         .Include(s => s.ProgcodeNavigation)
    //         .Where(s => s.Instcode == institute)
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
    //         }).ToListAsync();
    //     
    //     return students;
    // }
    

}
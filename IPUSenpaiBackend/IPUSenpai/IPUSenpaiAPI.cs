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
    
    public async Task<Dictionary<string?, short>> GetInstitutes(short limit = 100)
    {
        /*
         * select instname, count(*) from institute inner join student on student.instcode=institute.instcode group by institute.instname having count(*) > 100 order by count(*) desc;
         */
        var institutes = await _context.Students
            .Include(s => s.InstcodeNavigation)
            .GroupBy(s => s.InstcodeNavigation.Instname)
            .Where(s => s.Count() > 100)
            .OrderByDescending(s => s.Count())
            .Select(s => new
            {
                Instname = s.Key,
                Instcode = s.FirstOrDefault().InstcodeNavigation.Instcode
            })
            .ToDictionaryAsync(s => s.Instname, s => s.Instcode);
        // Total 107 unique institutes
        // var institutes = (from s in _context.Institutes
        //     join i in _context.Students on s.Instcode equals i.Instcode
        //     group s by s.Instname into g
        //     // where g.Count() > 500
        //     orderby g.Count() descending
        //     select g.Key).Take(limit);
        return institutes;
    }
    
    public async Task<List<string?>> GetInstitutesByProgramme(string programme, short limit = 100)
    {
        var programmes = await _context.ProgrammesInstitutes
            .Include(pi => pi.InstcodeNavigation)
            .Where(pi => pi.ProgcodeNavigation.Prog == programme)
            .Select(pi => pi.InstcodeNavigation.Instname)
            .Distinct()
            .OrderBy(instname => instname)
            .ToListAsync();

        if (programmes.Count == 0)
        {
            programmes.Add("No programmes found");
        }
        return programmes;
    }

    public async Task<List<string?>> GetProgrammes(short limit = 79)
    {
        var programmes = await _context.Programmes
            .GroupBy(p => p.Prog)
            .Select(p => p.Key)
            .Take(limit)
            .ToListAsync();
        if (programmes.Count == 0)
        {
            programmes.Add("No programmes found");
        }
        return programmes;
    }
    
    public async Task<Dictionary<string, string>> GetSpecializationsByProgramme(short limit = 30, string? prog = "")
    {
        var specializations = await _context.Programmes
        .Where(p => p.Prog == prog)
        .Select(p => new
        {
            p.Spec,
            p.Progcode
        })
        .Take(limit)
        .ToDictionaryAsync(p => p.Spec, p => p.Progcode);
        if (specializations.Count == 0)
        {
            specializations.Add("No specializations found", "0");
        }
        return specializations;
    }
    
    public async Task<Dictionary<string, short>> GetInstituteCodesForShifts(string instname)
    {
        var institutes = await _context.Institutes
        .Where(i => i.Instname == instname)
        .Select(i => i.Instcode)
        .OrderBy(s => s)
        .ToListAsync();
        
        Dictionary<string, short> shifts = new();
        
        /*
         * Assign shifts to each institute code, the smallest institute code gets the morning shift
         * and the next smallest gets the evening shift
         * and the last one gets the other shift
         */
        if (institutes.Count > 0)
        {
            shifts.Add("Morning", institutes[0]);
            if (institutes.Count > 1)
            {
                shifts.Add("Evening", institutes[1]);
            }
            if (institutes.Count > 2)
            {
                shifts.Add("Other", institutes[2]);
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
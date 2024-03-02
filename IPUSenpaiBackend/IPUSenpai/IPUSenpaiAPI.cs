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

        return student;

    }

}
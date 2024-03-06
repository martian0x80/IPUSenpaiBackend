using IPUSenpaiBackend.IPUSenpai;
using IPUSenpaiBackend.CustomEntities;
using Microsoft.AspNetCore.Mvc;
// using Microsoft.AspNetCore.RateLimiting;

namespace IPUSenpaiBackend.Controllers;

[ApiController]
[Route("")]
public class IPUSenpaiController : ControllerBase
{
    private readonly IIPUSenpaiAPI _api;
    private readonly ILogger _logger;
    
    public IPUSenpaiController(IIPUSenpaiAPI api, ILogger<IPUSenpaiController> logger)
    {
        _api = api;
        _logger = logger;
    }
    
    [HttpGet]
    [Route("/")]
    public string Get()
    {
        return "Pani Puri Senpai API v1.0.0";
    }
    
    [HttpGet]
    [Route("student/{enrollment}")]
    // [EnableRateLimiting("tokenbucket")]
    public async Task<StudentSenpai> GetStudent(string enrollment)
    {
        if (enrollment.Length < 10)
        {
            return new StudentSenpai();
        }
        return await _api.GetStudentByEnrollment(enrollment);
    }
    
    [HttpGet]
    [Route("programmes/{limit?}")]
    public async Task<List<PartialResponse>> GetProgrammes(short limit = 79)
    {
        return await _api.GetProgrammes(limit);
    }
    
    [HttpGet]
    [Route("institutes/{limit?}")]
    public async Task<List<Response>> GetInstitutes(short limit = 50)
    {
        return await _api.GetInstitutes(limit);
    }
    
    [HttpGet]
    [Route("institutes/programme={programme}/{limit?}")]
    public async Task<List<PartialResponse>> GetInstitutes(string programme, short limit = 100)
    {
        return await _api.GetInstitutesByProgramme(programme, limit);
    }
    
    [HttpGet]
    [Route("specializations/programme={programme}&institute={instname}/{limit?}")]
    public async Task<List<Response>> GetSpecializations(string programme, string instname, short limit = 100)
    {
        return await _api.GetSpecializationsByProgrammeAndInstname(limit, programme, instname);
    }
    
    [HttpGet]
    [Route("institute/shifts/{instname}")]
    public Task<List<Response>> GetInstituteShifts(string instname)
    {
        return _api.GetInstituteCodesForShifts(instname);
    }
    
    [HttpGet]
    [Route("batches/programme={programme}&institute={institute}")]
    public async Task<List<Response>> GetBatches(string programme, string institute)
    {
        Dictionary<string, int> courseDurations = new Dictionary<string, int>()
        {
            ["BACHELOR OF ARTS"] = 3,
            ["BACHELOR OF ARTS HONOURS"] = 4,
            ["BACHELOR OF AUDIOLOGY AND SPEECH LANGUAGE PATHOLOGY"] = 4,
            ["BACHELOR OF BUSINESS ADMINISTRATION"] = 3,
            ["BACHELOR OF COMMERCE"] = 3,
            ["BACHELOR OF COMPUTER APPLICATIONS"] = 3,
            ["BACHELOR OF EDUCATION"] = 4,
            ["BACHELOR OF TECHNOLOGY"] = 4,
            ["I HAVE NO IDEA?"] = -1, // Use -1 or any placeholder value for unknown durations
            ["INTEGRATED"] = 5,
            ["MASTER OF BUSINESS ADMINISTRATION"] = 2,
            ["MASTER OF COMPUTER APPLICATIONS"] = 2,
            ["MASTER OF TECHNOLOGY"] = 2
        };
        var batches = await _api.GetBatchesByPrognameAndInstname(programme, institute);
        var batchMap = new List<Response>();
        
        if (courseDurations.ContainsKey(programme))
        {
            int duration = courseDurations[programme];
            for (int i = 0; i < batches.Count; i++)
            {
                if (batches[i] != null)
                {
                    batchMap.Add(new Response
                    {
                        Name = $"{batches[i]}-{((short)(batches[i] + duration))}",
                        Value = batches[i].ToString()
                    
                    });
                }
            }
        }

        return batchMap;
    }
    
    [HttpGet]
    [Route("semesters/programme={programme}&institute={institute}")]
    public async Task<List<PartialResponse>> GetSemesters(string programme, string institute)
    {
        return await _api.GetSemestersByProgrammeAndInstname(programme, institute);
    }
    
    [HttpGet]
    [Route("institute/{instcode}")]
    public async Task<InstituteSenpai> GetInstitute(short instcode)
    {
        return await _api.GetInstituteByInstcode(instcode);
    }
    
    [HttpGet]
    [Route("programme/{progcode}")]
    public async Task<ProgrammeSenpai> GetProgramme(string progcode)
    {
        return await _api.GetProgrammeByProgcode(progcode);
    }
}

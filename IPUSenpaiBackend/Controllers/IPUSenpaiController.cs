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
    public async Task<List<String?>> GetProgrammes(short limit = 79)
    {
        return await _api.GetProgrammes(limit);
    }
    
    [HttpGet]
    [Route("institutes/{limit?}")]
    public async Task<Dictionary<string?, short>> GetInstitutes(short limit = 50)
    {
        return await _api.GetInstitutes(limit);
    }
    
    [HttpGet]
    [Route("institutes/programme={programme}/{limit?}")]
    public async Task<List<string?>> GetInstitutes(string programme, short limit = 100)
    {
        return await _api.GetInstitutesByProgramme(programme, limit);
    }
    
    [HttpGet]
    [Route("specializations/programme={programme}/{limit?}")]
    public async Task<Dictionary<string, string>> GetSpecializations(string programme, short limit = 100)
    {
        return await _api.GetSpecializationsByProgramme(limit, programme);
    }
    
    [HttpGet]
    [Route("institute/shifts/{instname}")]
    public Task<Dictionary<string, short>> GetInstituteShifts(string instname)
    {
        return _api.GetInstituteCodesForShifts(instname);
    }
    
    [HttpGet]
    [Route("batches/programme={programme}&institute={institute}")]
    public async Task<List<short?>> GetBatches(string programme, string institute)
    {
        return await _api.GetBatchesByPrognameAndInstname(programme, institute);
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
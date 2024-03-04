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
    [Route("institutes/{limit:int?}")]
    public async Task<List<String?>> GetInstitutes(short limit)
    {
        return await _api.GetInstitutes(limit);
    }
    
    [HttpGet]
    [Route("programmes/{limit:int?}")]
    public async Task<List<String?>> GetProgrammes(short limit)
    {
        return await _api.GetProgrammes(limit);
    }
    
    [HttpGet]
    [Route("specializations/programme={programme}/{limit:int?}")]
    public async Task<List<String?>> GetSpecializations(short limit, string programme)
    {
        return await _api.GetSpecializations(limit, programme);
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
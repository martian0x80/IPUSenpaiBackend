using IPUSenpaiBackend.IPUSenpai;
using IPUSenpaiBackend.CustomEntities;
using Microsoft.AspNetCore.Mvc;

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
    public async Task<StudentSenpai> GetStudent(string enrollment)
    {
        if (enrollment.Length < 10)
        {
            return new StudentSenpai();
        }
        return await _api.GetStudentByEnrollment(enrollment);
    }
}
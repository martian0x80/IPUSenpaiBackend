using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using IPUSenpaiBackend.IPUSenpai;
using IPUSenpaiBackend.CustomEntities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;

// using Microsoft.AspNetCore.RateLimiting;

namespace IPUSenpaiBackend.Controllers;

[ApiController]
[Route("")]
public class IPUSenpaiController : ControllerBase
{
    private readonly IIPUSenpaiAPI _api;
    private readonly ILogger _logger;
    private readonly IDistributedCache _cache;
    public JsonSerializerOptions SerializerOptions { get; set; } = new JsonSerializerOptions
    {
        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals
    };
    
    public IPUSenpaiController(IIPUSenpaiAPI api, ILogger<IPUSenpaiController> logger, IDistributedCache cache)
    {
        _api = api;
        _logger = logger;
        _cache = cache;
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
        var cachedProgrammes = await _cache.GetStringAsync("GetProgrammes");
        if (!string.IsNullOrEmpty(cachedProgrammes))
        {
            try {
                return JsonSerializer.Deserialize<List<PartialResponse>>(cachedProgrammes);
            }
            catch (JsonException e)
            {
                _logger.LogError(e, "Error deserializing cached programmes");
            }
        }
        var programmes = await _api.GetProgrammes(limit);
        await _cache.SetStringAsync("GetProgrammes", JsonSerializer.Serialize(programmes));
        return programmes;
    }
    
    [HttpGet]
    [Route("institutes/{limit?}")]
    public async Task<List<Response>> GetInstitutes(short limit = 50)
    {
        var cachedInstitutes = await _cache.GetStringAsync("GetInstitutes");
        if (!string.IsNullOrEmpty(cachedInstitutes))
        {
            try {
                return JsonSerializer.Deserialize<List<Response>>(cachedInstitutes);
            }
            catch (JsonException e)
            {
                _logger.LogError(e, "Error deserializing cached institutes");
            }
        }
        var institutes = await _api.GetInstitutes(limit);
        await _cache.SetStringAsync("GetInstitutes", JsonSerializer.Serialize(institutes));
        return institutes;
    }
    
    [HttpGet]
    [Route("institutes/programme={programme}/{limit?}")]
    public async Task<List<PartialResponse>> GetInstitutes(string programme, short limit = 100)
    {
        var cachedInstitutes = await _cache.GetStringAsync($"GetInstitutesByProgramme_{programme}_limit={limit}");
        if (!string.IsNullOrEmpty(cachedInstitutes))
        {
            try {
                return JsonSerializer.Deserialize<List<PartialResponse>>(cachedInstitutes);
            }
            catch (JsonException e)
            {
                _logger.LogError(e, "Error deserializing cached institutes by programme");
            }
        }
        var institutes = await _api.GetInstitutesByProgramme(programme, limit);
        await _cache.SetStringAsync($"GetInstitutesByProgramme_{programme}_limit={limit}", JsonSerializer.Serialize(institutes));
        return institutes;
    }
    
    [HttpGet]
    [Route("specializations/programme={programme}&institute={instname}/{limit?}")]
    public async Task<List<Response>> GetSpecializations(string programme, string instname, short limit = 100)
    {
        var cachedSpecializations = await _cache.GetStringAsync($"GetSpecializationsByProgrammeAndInstname_{programme}_{instname}_limit={limit}");
        if (!string.IsNullOrEmpty(cachedSpecializations))
        {
            try {
                return JsonSerializer.Deserialize<List<Response>>(cachedSpecializations);
            }
            catch (JsonException e)
            {
                _logger.LogError(e, "Error deserializing cached specializations by programme and institute");
            }
        }
        var specializations = await _api.GetSpecializationsByProgrammeAndInstname(limit, programme, instname);
        await _cache.SetStringAsync($"GetSpecializationsByProgrammeAndInstname_{programme}_{instname}_limit={limit}", JsonSerializer.Serialize(specializations));
        return specializations;
    }
    
    [HttpGet]
    [Route("institute/shifts/{instname}")]
    public Task<List<Response>> GetInstituteShifts(string instname)
    {
        var cachedShifts = _cache.GetString($"GetInstituteShifts_{instname}");
        if (!string.IsNullOrEmpty(cachedShifts))
        {
            try {
                return Task.FromResult(JsonSerializer.Deserialize<List<Response>>(cachedShifts));
            }
            catch (JsonException e)
            {
                _logger.LogError(e, "Error deserializing cached institute shifts");
            }
        }
        var shifts = _api.GetInstituteCodesForShifts(instname);
        _cache.SetString($"GetInstituteShifts_{instname}", JsonSerializer.Serialize(shifts));
        return shifts;
    }
    
    [HttpGet]
    [Route("batches/programme={programme}&institute={institute}")]
    public async Task<List<Response>> GetBatches(string programme, string institute)
    {
        var cachedBatches = await _cache.GetStringAsync($"GetBatchesByPrognameAndInstname_{programme}_{institute}");
        if (!string.IsNullOrEmpty(cachedBatches))
        {
            try {
                return JsonSerializer.Deserialize<List<Response>>(cachedBatches);
            }
            catch (JsonException e)
            {
                _logger.LogError(e, "Error deserializing cached batches by programme and institute");
            }
        }

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
            ["I HAVE NO IDEA?"] = -1,
            ["INTEGRATED"] = 5,
            ["MASTER OF BUSINESS ADMINISTRATION"] = 2,
            ["MASTER OF COMPUTER APPLICATIONS"] = 2,
            ["MASTER OF TECHNOLOGY"] = 2,
            ["MASTER OF ARCHITECTURE"] = 2, // Assuming a 2-year program
            ["MASTER OF EDUCATION"] = 2, // Assuming a 2-year program
            ["MASTER OF LAW"] = 2, // Assuming a 2-year program
            ["POST GRADUATE DIPLOMA IN DATA ANALYTICS"] = 1, // Assuming a 1-year program
            ["BACHELOR OF DESIGN"] = 4, // Assuming a 4-year program
            ["BACHELOR OF SCIENCE"] = 3, // Assuming a 3-year program
            ["FOUR-YEARS BACHELOR OF ARTS"] = 4,
            ["MASTER OF PLANNING"] = 2, // Assuming a 2-year program
            ["BACHELOR OF HOTEL MANAGEMENT AND CATERING TECHNOLOGY"] = 4, // Assuming a 4-year program
            ["MASTER OF ARTS"] = 2, // Assuming a 2-year program
            ["BACHELOR OF PHARMACY"] = 4, // Assuming a 4-year program
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
        var serializedBatches = JsonSerializer.Serialize(batchMap);
        await _cache.SetStringAsync($"GetBatchesByPrognameAndInstname_{programme}_{institute}", serializedBatches);
        return batchMap;
    }
    
    [HttpGet]
    [Route("semesters/programme={programme}&institute={institute}&batch={batch}")]
    public async Task<List<PartialResponse>> GetSemesters(string programme, string institute, string batch)
    {
        var cachedSemesters = await _cache.GetStringAsync($"GetSemestersByProgrammeAndInstname_{programme}_{institute}_{batch}");
        if (!string.IsNullOrEmpty(cachedSemesters))
        {
            try {
                return JsonSerializer.Deserialize<List<PartialResponse>>(cachedSemesters);
            }
            catch (JsonException e)
            {
                _logger.LogError(e, "Error deserializing cached semesters by programme and institute");
            }
        }
        var semesters = await _api.GetSemestersByProgrammeInstnameBatch(programme, institute, batch);
        await _cache.SetStringAsync($"GetSemestersByProgrammeAndInstname_{programme}_{institute}_{batch}", JsonSerializer.Serialize(semesters));
        return semesters;
    }
    
    [HttpGet]
    [Route("rank/semester/instcode={instcode}&progcode={progcode}&batch={batch}&sem={sem}&pageNumber={pageNumber}&pageSize={pageSize}")]
    public List<RankSenpaiSemester> GetRankSem(string instcode, string progcode, string batch, string sem, int pageNumber, int pageSize)
    {
        var cachedRank = _cache.GetString($"GetRanklistBySemester_{instcode}_{progcode}_{batch}_{sem}_pageNumber={pageNumber}_pageSize={pageSize}");
        IHeaderDictionary headers = Response.Headers;
        if (!string.IsNullOrEmpty(cachedRank))
        {
            try {
                var rank = JsonSerializer.Deserialize<Tuple<List<RankSenpaiSemester>, int>>(cachedRank);
                var rankList = rank.Item1;
                headers.Append("X-Total-Page-Count", rank.Item2.ToString());
                _logger.LogInformation("\n[I] Returning cached ranklist by semester");
                return rankList;
            }
            catch (JsonException e)
            {
                _logger.LogError(e, "Error deserializing cached ranklist by semester");
            }
        }
        var resp = _api.GetRanklistBySemester(instcode, progcode, batch, sem, pageNumber, pageSize);
        var pageCount = (int)Math.Ceiling((double)resp.Item2 / pageSize);
        _cache.SetString(
            $"GetRanklistBySemester_{instcode}_{progcode}_{batch}_{sem}_pageNumber={pageNumber}_pageSize={pageSize}",
            JsonSerializer.Serialize(new Tuple<List<RankSenpaiSemester>, int>(resp.Item1, pageCount), SerializerOptions));
        _logger.LogInformation("\n[I] Returning fresh ranklist by semester");
        if (headers.ContainsKey("X-Total-Page-Count"))
        {
            headers.Remove("X-Total-Page-Count");
        }
        headers.Append("X-Total-Page-Count", pageCount.ToString());
        return resp.Item1;
    }
    
    [HttpGet]
    [Route("rank/instcode={instcode}&progcode={progcode}&batch={batch}&pageNumber={pageNumber}&pageSize={pageSize}")]
    public List<RankSenpaiOverall> GetRank(string instcode, string progcode, string batch, int pageNumber, int pageSize)
    {
        var cachedRank = _cache.GetString($"GetRanklistOverall_{instcode}_{progcode}_{batch}_pageNumber={pageNumber}_pageSize={pageSize}");
        IHeaderDictionary headers = Response.Headers;
        if (!string.IsNullOrEmpty(cachedRank))
        {
            try {
                var rank = JsonSerializer.Deserialize<Tuple<List<RankSenpaiOverall>, int>>(cachedRank);
                var rankList = rank.Item1;
                headers.Append("X-Total-Page-Count", rank.Item2.ToString());
                _logger.LogInformation("\n[I] Returning cached ranklist overall");
                return rankList;
            }
            catch (JsonException e)
            {
                _logger.LogError(e, "Error deserializing cached ranklist overall");
            }
        }
        var resp =  _api.GetRanklistOverall(instcode, progcode, batch, pageNumber, pageSize);
        var pageCount = (int)Math.Ceiling((double)resp.Item2 / pageSize);
        _cache.SetString(
            $"GetRanklistOverall_{instcode}_{progcode}_{batch}_pageNumber={pageNumber}_pageSize={pageSize}",
            JsonSerializer.Serialize(new Tuple<List<RankSenpaiOverall>, int>(resp.Item1, pageCount), SerializerOptions));
        _logger.LogInformation("\n[I] Returning fresh ranklist by semester");
        if (headers.ContainsKey("X-Total-Page-Count"))
        {
            headers.Remove("X-Total-Page-Count");
        }
        headers.Append("X-Total-Page-Count", pageCount.ToString());
        return resp.Item1;
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

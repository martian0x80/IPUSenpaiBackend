using System.Text.Json;
using System.Text.RegularExpressions;
using IPUSenpaiBackend.IPUSenpai;
using IPUSenpaiBackend.CustomEntities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Caching.Distributed;

namespace IPUSenpaiBackend.Controllers;

[ApiController]
[Route("")]
public class IPUSenpaiController : ControllerBase
{
    private readonly IIPUSenpaiAPI _api;
    private readonly ILogger _logger;
    private readonly IDistributedCache _cache;
    private readonly bool _enableCache = false;

    public readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
    {
        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals
    };

    public readonly DistributedCacheEntryOptions CacheOptions = new DistributedCacheEntryOptions
    {
        // AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60 * 60 * 24 * 30 * 6) // 6 months
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
    [Route("programmes/{limit?}")]
    public async Task<List<PartialResponse>> GetProgrammes(short limit = 79)
    {
        if (_enableCache)
        {
            var cachedProgrammes = await _cache.GetStringAsync("GetProgrammes");
            if (!string.IsNullOrEmpty(cachedProgrammes))
            {
                try
                {
                    return JsonSerializer.Deserialize<List<PartialResponse>>(cachedProgrammes) ??
                           new List<PartialResponse>();
                }
                catch (JsonException e)
                {
                    _logger.LogError(e, "Error deserializing cached programmes");
                }
            }
        }

        var programmes = await _api.GetProgrammes(limit);
        if (_enableCache)
        {
            await _cache.SetStringAsync("GetProgrammes", JsonSerializer.Serialize(programmes), CacheOptions);
        }

        return programmes;
    }

    [HttpGet]
    [Route("institutes/{limit?}")]
    public async Task<List<Response>> GetInstitutes(short limit = 50)
    {
        if (_enableCache)
        {
            var cachedInstitutes = await _cache.GetStringAsync("GetInstitutes");
            if (!string.IsNullOrEmpty(cachedInstitutes))
            {
                try
                {
                    return JsonSerializer.Deserialize<List<Response>>(cachedInstitutes) ?? new List<Response>();
                }
                catch (JsonException e)
                {
                    _logger.LogError(e, "Error deserializing cached institutes");
                }
            }
        }

        var institutes = await _api.GetInstitutes(limit);
        if (_enableCache)
        {
            await _cache.SetStringAsync("GetInstitutes", JsonSerializer.Serialize(institutes), CacheOptions);
        }

        return institutes;
    }

    [HttpGet]
    [Route("institutes/programme={programme}/{limit?}")]
    public async Task<List<PartialResponse>> GetInstitutes(string programme, short limit = 100)
    {
        if (_enableCache)
        {
            var cachedInstitutes = await _cache.GetStringAsync($"GetInstitutesByProgramme_{programme}_limit={limit}");
            if (!string.IsNullOrEmpty(cachedInstitutes))
            {
                try
                {
                    return JsonSerializer.Deserialize<List<PartialResponse>>(cachedInstitutes) ??
                           new List<PartialResponse>();
                }
                catch (JsonException e)
                {
                    _logger.LogError(e, "Error deserializing cached institutes by programme");
                }
            }
        }

        var institutes = await _api.GetInstitutesByProgramme(programme, limit);
        if (_enableCache)
        {
            await _cache.SetStringAsync($"GetInstitutesByProgramme_{programme}_limit={limit}",
                JsonSerializer.Serialize(institutes), CacheOptions);
        }

        return institutes;
    }

    [HttpGet]
    [Route("specializations/{limit?}")]
    public async Task<List<Response>> GetSpecializations(short limit = 200)
    {
        if (_enableCache)
        {
            var cachedSpecializations = await _cache.GetStringAsync($"GetSpecializations_limit={limit}");
            if (!string.IsNullOrEmpty(cachedSpecializations))
            {
                try
                {
                    return JsonSerializer.Deserialize<List<Response>>(cachedSpecializations) ?? new List<Response>();
                }
                catch (JsonException e)
                {
                    _logger.LogError(e, "Error deserializing cached specializations");
                }
            }
        }

        var specializations = await _api.GetSpecializations(limit);
        if (_enableCache)
        {
            await _cache.SetStringAsync($"GetSpecializations_limit={limit}", JsonSerializer.Serialize(specializations),
                CacheOptions);
        }

        return specializations;
    }

    [HttpGet]
    [Route("specializations/programme={programme}&institute={instname}/{limit?}")]
    public async Task<List<Response>> GetSpecializations(string programme, string instname, short limit = 100)
    {
        if (_enableCache)
        {
            var cachedSpecializations =
                await _cache.GetStringAsync(
                    $"GetSpecializationsByProgrammeAndInstname_{programme}_{instname}_limit={limit}");
            if (!string.IsNullOrEmpty(cachedSpecializations))
            {
                try
                {
                    return JsonSerializer.Deserialize<List<Response>>(cachedSpecializations) ?? new List<Response>();
                }
                catch (JsonException e)
                {
                    _logger.LogError(e, "Error deserializing cached specializations by programme and institute");
                }
            }
        }

        var specializations = await _api.GetSpecializationsByProgrammeAndInstname(limit, programme, instname);
        if (_enableCache)
        {
            await _cache.SetStringAsync(
                $"GetSpecializationsByProgrammeAndInstname_{programme}_{instname}_limit={limit}",
                JsonSerializer.Serialize(specializations), CacheOptions);
        }

        return specializations;
    }

    [HttpGet]
    [Route("institute/shifts/{instname}")]
    public async Task<List<Response>> GetInstituteShifts(string instname)
    {
        if (_enableCache)
        {
            var cachedShifts = await _cache.GetStringAsync($"GetInstituteShifts_{instname}");
            if (!string.IsNullOrEmpty(cachedShifts))
            {
                try
                {
                    return JsonSerializer.Deserialize<List<Response>>(cachedShifts) ?? new List<Response>();
                }
                catch (JsonException e)
                {
                    _logger.LogError(e, "Error deserializing cached institute shifts");
                }
            }
        }

        var shifts = await _api.GetInstituteCodesForShifts(instname);
        if (_enableCache)
        {
            await _cache.SetStringAsync($"GetInstituteShifts_{instname}", JsonSerializer.Serialize(shifts),
                CacheOptions);
        }

        return shifts;
    }

    [HttpGet]
    [Route("batches/programme={programme}&institute={institute}")]
    public async Task<List<Response>> GetBatches(string programme, string institute)
    {
        if (_enableCache)
        {
            var cachedBatches = await _cache.GetStringAsync($"GetBatchesByPrognameAndInstname_{programme}_{institute}");
            if (!string.IsNullOrEmpty(cachedBatches))
            {
                try
                {
                    return JsonSerializer.Deserialize<List<Response>>(cachedBatches) ?? new List<Response>();
                }
                catch (JsonException e)
                {
                    _logger.LogError(e, "Error deserializing cached batches by programme and institute");
                }
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
            ["I HAVE NO IDEA?"] = 0,
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
        if (_enableCache)
        {
            await _cache.SetStringAsync($"GetBatchesByPrognameAndInstname_{programme}_{institute}", serializedBatches,
                CacheOptions);
        }

        return batchMap;
    }

    [HttpGet]
    [Route("semesters/programme={programme}&institute={institute}&batch={batch}")]
    public async Task<List<PartialResponse>> GetSemesters(string programme, string institute, string batch)
    {
        if (_enableCache)
        {
            var cachedSemesters =
                await _cache.GetStringAsync($"GetSemestersByProgrammeAndInstname_{programme}_{institute}_{batch}");
            if (!string.IsNullOrEmpty(cachedSemesters))
            {
                try
                {
                    return JsonSerializer.Deserialize<List<PartialResponse>>(cachedSemesters) ??
                           new List<PartialResponse>();
                }
                catch (JsonException e)
                {
                    _logger.LogError(e, "Error deserializing cached semesters by programme and institute");
                }
            }
        }

        var semesters = await _api.GetSemestersByProgrammeInstnameBatch(programme, institute, batch);
        if (_enableCache)
        {
            await _cache.SetStringAsync($"GetSemestersByProgrammeAndInstname_{programme}_{institute}_{batch}",
                JsonSerializer.Serialize(semesters), CacheOptions);
        }

        return semesters;
    }

    [HttpGet]
    [Route(
        "rank/semester/instcode={instcode}&progcode={progcode}&batch={batch}&sem={sem}&pageNumber={pageNumber}&pageSize={pageSize}/{instname?}")]
    public RankSenpaiSemesterResponse GetRankSem(string instcode, string? instname, string progcode, string batch,
        string sem, int pageNumber = 1, int pageSize = 60)
    {
        // if (instcode == "*" && instname == null)
        // {
        //     throw new Exception("Institute name is required when instcode is *");
        // }
        IHeaderDictionary headers = Response.Headers;
        if (_enableCache)
        {
            var cachedRank =
                _cache.GetString(
                    $"GetRanklistBySemester_{instcode}_{progcode}_{batch}_{sem}_pageNumber={pageNumber}_pageSize={pageSize}_{instname}");
            if (!string.IsNullOrEmpty(cachedRank))
            {
                try
                {
                    var rank =
                        JsonSerializer
                            .Deserialize<Tuple<List<RankSenpaiSemester>, int, float, float, List<GpaListResponse>>>(
                                cachedRank);
                    var rankList = rank?.Item1;
                    headers.Append("X-Total-Page-Count", rank?.Item2.ToString());
                    _logger.LogInformation("\n[I] Returning cached ranklist by semester");
                    return new RankSenpaiSemesterResponse
                        { Ranklist = rankList, AvgGpa = rank.Item3, GpaList = rank.Item5, AvgPercentage = rank.Item4 };
                }
                catch (JsonException e)
                {
                    _logger.LogError(e, "Error deserializing cached ranklist by semester");
                }
            }
        }

        var resp = _api.GetRanklistBySemester(instcode, instname, progcode, batch, sem, pageNumber, pageSize);
        var pageCount = (int)Math.Ceiling((double)resp.Item2 / pageSize);
        if (_enableCache)
        {
            _cache.SetString(
                $"GetRanklistBySemester_{instcode}_{progcode}_{batch}_{sem}_pageNumber={pageNumber}_pageSize={pageSize}_{instname}",
                JsonSerializer.Serialize(
                    new Tuple<List<RankSenpaiSemester>, int, float, float, List<GpaListResponse>>(resp.Item1, pageCount,
                        resp.Item3,
                        resp.Item4,
                        resp.Item5),
                    SerializerOptions), CacheOptions);
        }

        _logger.LogInformation("\n[I] Returning fresh ranklist by semester");
        if (headers.ContainsKey("X-Total-Page-Count"))
        {
            headers.Remove("X-Total-Page-Count");
        }

        headers.Append("X-Total-Page-Count", pageCount.ToString());
        return new RankSenpaiSemesterResponse
            { Ranklist = resp.Item1, AvgGpa = resp.Item3, GpaList = resp.Item5, AvgPercentage = resp.Item4 };
    }

    [HttpGet]
    [Route(
        "rank/instcode={instcode}&progcode={progcode}&batch={batch}&pageNumber={pageNumber}&pageSize={pageSize}/{instname?}")]
    [OutputCache]
    public RankSenpaiOverallResponse GetRank(string instcode, string? instname, string progcode, string batch,
        int pageNumber, int pageSize)
    {
        // if (instcode == "*" && instname == null)
        // {
        //     throw new Exception("Institute name is required when instcode is *");
        // }
        IHeaderDictionary headers = Response.Headers;
        if (_enableCache)
        {
            var cachedRank =
                _cache.GetString(
                    $"GetRanklistOverall_{instcode}_{progcode}_{batch}_pageNumber={pageNumber}_pageSize={pageSize}_{instname}");
            if (!string.IsNullOrEmpty(cachedRank))
            {
                try
                {
                    var rank =
                        JsonSerializer
                            .Deserialize<Tuple<List<RankSenpaiOverall>, int, float, float, List<GpaListResponse>>>(
                                cachedRank);
                    var rankList = rank?.Item1;
                    headers.Append("X-Total-Page-Count", rank.Item2.ToString());
                    _logger.LogInformation("\n[I] Returning cached ranklist overall");
                    return new RankSenpaiOverallResponse
                        { Ranklist = rankList, AvgGpa = rank.Item3, GpaList = rank.Item5, AvgPercentage = rank.Item4 };
                }
                catch (JsonException e)
                {
                    _logger.LogError(e, "Error deserializing cached ranklist overall");
                }
            }
        }

        var resp = _api.GetRanklistOverall(instcode, instname, progcode, batch, pageNumber, pageSize);
        var pageCount = (int)Math.Ceiling((double)resp.Item2 / pageSize);
        if (_enableCache)
        {
            _cache.SetString(
                $"GetRanklistOverall_{instcode}_{progcode}_{batch}_pageNumber={pageNumber}_pageSize={pageSize}_{instname}",
                JsonSerializer.Serialize(
                    new Tuple<List<RankSenpaiOverall>, int, float, float, List<GpaListResponse>>(resp.Item1, pageCount,
                        resp.Item3,
                        resp.Item4,
                        resp.Item5),
                    SerializerOptions), CacheOptions);
        }

        _logger.LogInformation("\n[I] Returning fresh ranklist by semester");
        if (headers.ContainsKey("X-Total-Page-Count"))
        {
            headers.Remove("X-Total-Page-Count");
        }

        headers.Append("X-Total-Page-Count", pageCount.ToString());
        return new RankSenpaiOverallResponse
            { Ranklist = resp.Item1, AvgGpa = resp.Item3, GpaList = resp.Item5, AvgPercentage = resp.Item4 };
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

    [HttpGet]
    [Route("student/{enrollment}")]
    public IActionResult GetStudent(string enrollment)
    {
        if (_enableCache)
        {
            var cachedStudent = _cache.GetString($"GetStudent_{enrollment}");
            if (!string.IsNullOrEmpty(cachedStudent))
            {
                try
                {
                    _logger.LogInformation("\n[I] Returning cached student");
                    return Ok(JsonSerializer.Deserialize<StudentSenpai>(cachedStudent));
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error deserializing cached student");
                }
            }
        }

        var student = _api.GetStudent(enrollment);
        if (student == null)
        {
            return NotFound(new
            {
                message =
                    "S-Student ki jankaawi ^w^ nyahi miwi, OwO bhai.\nInvawid *runs away* ow (・`ω\u00b4・) nyon-existent *sweats* e-enwowwment nyumbew."
            });
        }

        if (_enableCache)
        {
            _cache.SetString($"GetStudent_{enrollment}", JsonSerializer.Serialize(student),
                new DistributedCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromMinutes(60 * 24)
                });
        }

        return Ok(student);
    }

    [HttpGet]
    //[Route("student/search/name={Name}&institute={Institute}&prog={Programme}&batch={Batch}")]
    [Route("student/search/{name}")]
    public async Task<IActionResult> SearchStudent(string name = "", string? institute = "",
        string? programme = "", string? batch = "")
    {
        if (name.Length < 3)
        {
            return BadRequest(new { message = "Name must be at least 3 characters long" });
        }

        // if (!Regex.IsMatch(name, @"[a-zA-Z\s]"))
        // {
        //     return BadRequest(new { message = "Name must contain only ASCII characters" });
        // }

        if (_enableCache)
        {
            var cachedSearch = await _cache.GetStringAsync($"SearchStudent_{name}_{institute}_{programme}_{batch}");
            if (!string.IsNullOrEmpty(cachedSearch))
            {
                try
                {
                    return Ok(JsonSerializer.Deserialize<List<StudentSearchSenpai>>(cachedSearch));
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error deserializing cached student search");
                }
            }
        }

        var search = await _api.GetSearchStudent(new StudentSearchFilterOptionsSenpai
        {
            Name = name,
            Institute = institute,
            Programme = programme,
            Batch = batch,
        });
        if (_enableCache)
        {
            await _cache.SetStringAsync($"SearchStudent_{name}_{institute}_{programme}_{batch}",
                JsonSerializer.Serialize(search), CacheOptions);
        }

        return Ok(search);
    }

    [HttpGet]
    [Route("count/")]
    public async Task<Dictionary<string, int>> GetCounts()
    {
        if (_enableCache)
        {
            var cachedCount = await _cache.GetStringAsync("GetCounts");
            if (!string.IsNullOrEmpty(cachedCount))
            {
                try
                {
                    return JsonSerializer.Deserialize<Dictionary<string, int>>(cachedCount) ??
                           new Dictionary<string, int>();
                }
                catch (JsonException e)
                {
                    _logger.LogError(e, "Error deserializing cached counts");
                }
            }
        }

        var count = await _api.GetCounts();

        if (_enableCache)
        {
            await _cache.SetStringAsync("GetCounts", JsonSerializer.Serialize(count), CacheOptions);
        }

        return count;
    }

    [HttpGet]
    [Route("count/by/{limit?}")]
    public async Task<StudentCountBy> GetCountsBy(int limit = 10)
    {
        if (_enableCache)
        {
            var cachedCount = await _cache.GetStringAsync($"GetCountsBy_{limit}");
            if (!string.IsNullOrEmpty(cachedCount))
            {
                try
                {
                    return JsonSerializer.Deserialize<StudentCountBy>(cachedCount) ??
                           new StudentCountBy();
                }
                catch (JsonException e)
                {
                    _logger.LogError(e, "Error deserializing cached counts by");
                }
            }
        }

        var count = await _api.GetCountsBy(limit);

        if (_enableCache)
        {
            await _cache.SetStringAsync($"GetCountsBy_{limit}", JsonSerializer.Serialize(count), CacheOptions);
        }

        return count;
    }

    [HttpGet]
    [Route("search/subjects/{query}/{limit?}")]
    public async Task<IActionResult> GetSearch(string query, int limit = 10)
    {
        var result = await _api.GetSearchSubjects(query, limit);
        return Ok(result);
    }
}
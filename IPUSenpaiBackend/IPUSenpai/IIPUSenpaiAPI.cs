using IPUSenpaiBackend.CustomEntities;

namespace IPUSenpaiBackend.IPUSenpai;

public interface IIPUSenpaiAPI
{
    public Task<List<Response>> GetInstitutes(short limit);
    public Task<List<PartialResponse>> GetInstitutesByProgramme(string programme, short limit = 79);
    public Task<List<PartialResponse>> GetProgrammes(short limit = 30);

    public Task<List<Response>> GetSpecializations(short limit = 30);

    //
    public Task<List<Response>> GetSpecializationsByProgrammeAndInstname(short limit = 30,
        string? programme = "BACHELOR OF TECHNOLOGY",
        string? instname = "University School of Information & Communication Technology");

    public Task<List<Response>> GetInstituteCodesForShifts(string instname);

    // The controller handles the Response wrapping for the next one
    public Task<List<short?>> GetBatchesByPrognameAndInstname(string programme, string institute);

    public Task<List<PartialResponse>> GetSemestersByProgrammeInstnameBatch(string programme, string institute,
        string batch);

    public Task<InstituteSenpai> GetInstituteByInstcode(short? instcode);
    public Task<ProgrammeSenpai> GetProgrammeByProgcode(string? progcode);

    public (List<RankSenpaiSemester>, int, float, float, List<GpaListResponse>) GetRanklistBySemester(string instcode,
        string? instname,
        string progcode,
        string batch, string sem, int pageNumber, int pageSize);

    public (List<RankSenpaiOverall>, int, float, float, List<GpaListResponse>) GetRanklistOverall(string instcode,
        string? instname,
        string progcode,
        string batch, int pageNumber, int pageSize);

    public StudentSenpai? GetStudent(string enrollment);
    public Task<List<StudentSearchSenpai>> GetSearchStudent(StudentSearchFilterOptionsSenpai filter);

//    No need to expose these methods
//    public Task<int> GetStudentCount();
//    public Task<int> GetResultCount();
//    public Task<int> GetProgrammeCount();
//    public Task<int> GetInstituteCount();
    public Task<Dictionary<string, int>> GetCounts();
    public Task<StudentCountBy> GetCountsBy(int limit);
    public Task<List<SubjectSenpaiFull>> GetSearchSubjects(string query, int limit);
}
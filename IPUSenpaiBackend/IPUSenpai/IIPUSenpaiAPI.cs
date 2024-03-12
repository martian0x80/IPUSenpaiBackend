using IPUSenpaiBackend.CustomEntities;
namespace IPUSenpaiBackend.IPUSenpai;

public interface IIPUSenpaiAPI
{
    public Task<StudentSenpai> GetStudentByEnrollment(string? enrollment);
    public Task<List<Response>> GetInstitutes(short limit = 79);
    public Task<List<PartialResponse>> GetInstitutesByProgramme(string programme, short limit = 79);
    public Task<List<PartialResponse>> GetProgrammes(short limit = 30);
    public Task<List<Response>> GetSpecializationsByProgrammeAndInstname(short limit = 30, string? programme = "BACHELOR OF TECHNOLOGY", string? instname = "University School of Information & Communication Technology");
    public Task<List<Response>> GetInstituteCodesForShifts(string instname);
    // The controller handles the Response wrapping for the next one
    public Task<List<short?>> GetBatchesByPrognameAndInstname(string programme, string institute);
    public Task<List<PartialResponse>> GetSemestersByProgrammeInstnameBatch(string programme, string institute, string batch);
    public Task<InstituteSenpai> GetInstituteByInstcode(short? instcode);
    public Task<ProgrammeSenpai> GetProgrammeByProgcode(string? progcode);

    public (List<RankSenpaiSemester>, int) GetRanklistBySemester(string instcode, string progcode, string batch, string sem, int pageNumber, int pageSize);
    public (List<RankSenpaiOverall>, int) GetRanklistOverall(string instcode, string progcode, string batch, int pageNumber, int pageSize);
}
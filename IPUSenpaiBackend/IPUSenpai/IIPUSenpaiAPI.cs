using System.Runtime.InteropServices.JavaScript;
using IPUSenpaiBackend.CustomEntities;
namespace IPUSenpaiBackend.IPUSenpai;

public interface IIPUSenpaiAPI
{
    public Task<StudentSenpai> GetStudentByEnrollment(string? enrollment);
    public Task<Dictionary<string?, short>> GetInstitutes(short limit = 79);
    public Task<List<string?>> GetInstitutesByProgramme(string programme, short limit = 79);
    public Task<List<string?>> GetProgrammes(short limit = 30);
    public Task<Dictionary<string, string>> GetSpecializationsByProgrammeAndInstname(short limit = 30, string? programme = "BACHELOR OF TECHNOLOGY", string? instname = "University School of Information & Communication Technology");
    public Task<Dictionary<string, short>> GetInstituteCodesForShifts(string instname);
    public Task<List<short?>> GetBatchesByPrognameAndInstname(string programme, string institute);
    public Task<InstituteSenpai> GetInstituteByInstcode(short? instcode);
    public Task<ProgrammeSenpai> GetProgrammeByProgcode(string? progcode);
}
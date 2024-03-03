using System.Runtime.InteropServices.JavaScript;
using IPUSenpaiBackend.CustomEntities;
namespace IPUSenpaiBackend.IPUSenpai;

public interface IIPUSenpaiAPI
{
    public Task<StudentSenpai> GetStudentByEnrollment(string? enrollment);
    public Task<List<string?>> GetInstitutes(short limit = 30);
    public Task<List<string?>> GetProgrammes(short limit = 30);
    public Task<List<string?>> GetSpecializations(short limit = 30, string? programme = null);
}
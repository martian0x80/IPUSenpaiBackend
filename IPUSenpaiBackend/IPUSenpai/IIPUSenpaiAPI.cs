using IPUSenpaiBackend.CustomEntities;
namespace IPUSenpaiBackend.IPUSenpai;

public interface IIPUSenpaiAPI
{
    public Task<StudentSenpai> GetStudentByEnrollment(string? enrollment);
}
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Services.UserImport;

public interface IUserSearchService
{
    Task<User[]> FindUsers(string firstName, string lastName, DateOnly dateofBirth, bool includeSynonyms = true);
}

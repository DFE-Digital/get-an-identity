using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Services.UserSearch;

public interface IUserSearchService
{
    Task<User[]> FindUsers(string firstName, string lastName, DateOnly? dateOfBirth, bool includeSynonyms = true);
}

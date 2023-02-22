namespace TeacherIdentity.AuthServer.Services.UserSearch;

public interface INameSynonymsService
{
    IReadOnlyCollection<string> GetSynonyms(string name);
}

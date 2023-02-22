namespace TeacherIdentity.AuthServer.Services.UserSearch;

public interface INameSynonymsService
{
    IList<string> GetSynonyms(string name);
}

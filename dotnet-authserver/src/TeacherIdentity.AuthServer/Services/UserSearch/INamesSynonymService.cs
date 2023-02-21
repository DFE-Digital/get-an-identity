namespace TeacherIdentity.AuthServer.Services.UserSearch;

public interface INamesSynonymService
{
    IList<string> GetSynonyms(string name);
}

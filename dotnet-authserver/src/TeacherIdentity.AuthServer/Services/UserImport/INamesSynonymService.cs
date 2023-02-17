namespace TeacherIdentity.AuthServer.Services.UserImport;

public interface INamesSynonymService
{
    IList<string> GetSynonyms(string name);
}

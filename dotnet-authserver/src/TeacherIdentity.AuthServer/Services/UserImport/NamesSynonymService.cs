namespace TeacherIdentity.AuthServer.Services.UserImport;

public class NamesSynonymService : INamesSynonymService
{
    private readonly Dictionary<string, HashSet<string>> namesLookup = new Dictionary<string, HashSet<string>>();

    public NamesSynonymService()
    {
        Initialise();
    }

    public IList<string> GetSynonyms(string name)
    {
        if (namesLookup.TryGetValue(name, out var synonyms))
        {
            return synonyms.ToList();
        }

        return new List<string>();
    }

    private void Initialise()
    {
        var namesFilePath = Path.Combine(AppContext.BaseDirectory, "names.csv");
        using var textReader = File.OpenText(namesFilePath);
        string? line = null;
        while ((line = textReader.ReadLine()) != null)
        {
            if (line.Length == 0 || line[0] == '#')
            {
                continue; // ignore empty lines and comments
            }

            var names = line.Split(',');
            foreach (var name in names)
            {
                HashSet<string>? synonyms;
                if (!namesLookup.TryGetValue(name, out synonyms))
                {
                    synonyms = new HashSet<string>();
                    namesLookup[name] = synonyms;
                }

                foreach (var altName in names)
                {
                    // Don't add anything as a synonym of itself
                    if (altName != name)
                    {
                        synonyms.Add(altName);
                    }
                }
            }
        }
    }
}

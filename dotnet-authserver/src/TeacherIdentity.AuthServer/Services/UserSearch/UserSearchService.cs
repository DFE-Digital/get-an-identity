using LinqKit;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Services.UserSearch;

public class UserSearchService : IUserSearchService
{
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly INameSynonymsService _namesSynonymService;

    public UserSearchService(
        TeacherIdentityServerDbContext dbContext,
        INameSynonymsService namesSynonymService)
    {
        _dbContext = dbContext;
        _namesSynonymService = namesSynonymService;
    }

    public async Task<User[]> FindUsers(string firstName, string lastName, DateOnly dateOfBirth, bool includeSynonyms = true)
    {
        var searchPredicate = PredicateBuilder.New<UserSearchAttribute>(false);

        if (includeSynonyms)
        {
            var synonyms = _namesSynonymService.GetSynonyms(firstName).ToList();
            synonyms.Insert(0, firstName);
            searchPredicate.Or(a => a.AttributeType == "first_name" && synonyms.Contains(a.AttributeValue));
        }
        else
        {
            searchPredicate.Or(a => a.AttributeType == "first_name" && a.AttributeValue == firstName);
        }

        searchPredicate.Or(a => a.AttributeType == "last_name" && a.AttributeValue == lastName);
        searchPredicate.Or(a => a.AttributeType == "date_of_birth" && a.AttributeValue == dateOfBirth.ToString("yyyy-MM-dd"));

        var results = await _dbContext.UserSearchAttributes
            .Where(searchPredicate)
            .GroupBy(a => a.UserId)
            .Where(g => g.Count() == 3)
            .Select(g => _dbContext.Users.Where(d => d.UserId == g.Key && d.UserType == UserType.Default).ToList())
            .ToListAsync();

        // each result (if there are any) is a list containing only a single user, so flatten it
        var users = results.SelectMany(u => u).ToArray();
        return users;
    }
}

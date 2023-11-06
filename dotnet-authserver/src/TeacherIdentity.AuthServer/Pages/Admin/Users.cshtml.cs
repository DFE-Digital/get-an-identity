using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using LinqKit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Pages.Admin;

[Authorize(AuthorizationPolicies.GetAnIdentitySupport)]
public class UsersModel : PageModel
{
    private const int PageSize = 100;

    private Expression<Func<User, UserInfo>> _mapUserInfo = user => new UserInfo()
    {
        UserId = user.UserId,
        EmailAddress = user.EmailAddress,
        Name = user.FirstName + " " + user.LastName,
        Trn = user.Trn,
        TrnLookupStatus = user.TrnLookupStatus,
        TrnLookupSupportTicketCreated = user.TrnLookupSupportTicketCreated
    };

    private readonly TeacherIdentityServerDbContext _dbContext;

    public UsersModel(TeacherIdentityServerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [Display(Name = "TRN lookup status")]
    [FromQuery(Name = "LookupStatus")]
    public TrnLookupStatus?[]? LookupStatus { get; set; }

    [Display(Name = "With support ticket")]
    [FromQuery(Name = "WithSupportTicket")]
    public bool WithSupportTicket { get; set; }

    [Display(Name = "Search")]
    [FromQuery(Name = "UserSearch")]
    public string? UserSearch { get; set; }

    public UserInfo[]? FilteredUsers { get; set; }

    public int TotalUsers { get; set; }

    [FromQuery(Name = "pageNumber")]
    public int? PageNumber { get; set; }

    public int[]? PaginationPages { get; set; }

    public int TotalPages { get; set; }

    public int? PreviousPage { get; set; }

    public int? NextPage { get; set; }

    public async Task<IActionResult> OnGet()
    {
        if (PageNumber < 1)
        {
            return BadRequest();
        }

        PageNumber ??= 1;

        Expression<Func<User, bool>> filterPredicate = await GetFilterPredicate(LookupStatus!, UserSearch);

        var sortedUsers = _dbContext.Users.OrderBy(u => u.LastName).ThenBy(u => u.FirstName);

        FilteredUsers = await sortedUsers.Where(filterPredicate)
            .Skip((PageNumber.Value - 1) * PageSize)
            .Take(PageSize)
            .Select(_mapUserInfo)
            .ToArrayAsync();

        TotalUsers = await sortedUsers.Where(filterPredicate).CountAsync();
        TotalPages = Math.Max((int)Math.Ceiling((decimal)TotalUsers / PageSize), 1);

        if (PageNumber > TotalPages)
        {
            // Page is out of range
            return BadRequest();
        }

        // In the pagination control, show the first page, last page, current page and two pages either side of the current page
        PaginationPages = Enumerable.Range(-2, 5).Select(offset => PageNumber.Value + offset)
            .Append(1)
            .Append(TotalPages)
            .Where(page => page <= TotalPages && page >= 1)
            .Distinct()
            .Order()
            .ToArray();

        PreviousPage = PageNumber > 1 ? PageNumber - 1 : null;
        NextPage = PageNumber < TotalPages ? PageNumber + 1 : null;

        return Page();
    }

    public Dictionary<string, string> GetPage(int pageNumber)
    {

        var pageData = new Dictionary<string, string>
        {
            { "PageNumber", pageNumber.ToString() },
            { "UserSearch", UserSearch ?? "" }
        };

        for (var i = 0; i < LookupStatus!.Length; i++)
        {
            pageData.Add($"LookupStatus[{i}]", LookupStatus[i].ToString()!);
        }
        return pageData;
    }

    public record UserInfo
    {
        public required Guid UserId { get; init; }
        public required string EmailAddress { get; init; }
        public required string Name { get; init; }
        public required string? Trn { get; init; }
        public required TrnLookupStatus? TrnLookupStatus { get; init; }
        public required bool TrnLookupSupportTicketCreated { get; init; }
    }

    private async Task<Expression<Func<User, bool>>> GetFilterPredicate(TrnLookupStatus?[] lookupStatus, string? userSearch)
    {
        var filterPredicate = PredicateBuilder.New<User>(user => user.UserType == UserType.Default);

        if (lookupStatus.Length > 0)
        {
            var lookupStatusWithoutPending = lookupStatus.Where(s => s != TrnLookupStatus.Pending).ToArray();
            var lookupStatusPredicate = PredicateBuilder.New<User>(user => lookupStatusWithoutPending.Contains(user.TrnLookupStatus));

            if (lookupStatus.Contains(TrnLookupStatus.Pending))
            {
                var pendingStatusPredicate = PredicateBuilder.New<User>(user => user.TrnLookupStatus == TrnLookupStatus.Pending);
                if (WithSupportTicket)
                {
                    pendingStatusPredicate.And(user => user.TrnLookupSupportTicketCreated == true);
                }

                lookupStatusPredicate.Or(pendingStatusPredicate);
            }

            filterPredicate.And(lookupStatusPredicate);
        }

        if (!string.IsNullOrEmpty(userSearch))
        {
            switch (userSearch)
            {
                case var searchString when new Regex(@"^\d{7}$").IsMatch(searchString):
                    filterPredicate.And(user => user.Trn == searchString);
                    break;
                case var searchString when new Regex(@".*@.*").IsMatch(searchString):
                    filterPredicate.And(user => user.EmailAddress == searchString);
                    break;
                default:
                    var userIds = await GetUsersByName(userSearch);
                    filterPredicate.And(user => userIds.Contains(user.UserId));
                    break;
            }
        }

        return filterPredicate;
    }

    private async Task<Guid[]> GetUsersByName(string searchString)
    {
        var names = searchString.Split(' ').Take(2).ToArray();

        var searchPredicate = PredicateBuilder.New<UserSearchAttribute>(
            a => a.AttributeType == "first_name" && a.AttributeValue == names[0]);

        if (names.Length == 2)
        {
            searchPredicate.Or(a => a.AttributeType == "last_name" && a.AttributeValue == names[1]);
        }
        else
        {
            searchPredicate.Or(a => a.AttributeType == "last_name" && a.AttributeValue == names[0]);
        }

        return await _dbContext.UserSearchAttributes
            .Where(searchPredicate)
            .GroupBy(a => a.UserId)
            .Where(g => g.Count() >= names.Length)
            .Select(g => g.Key)
            .ToArrayAsync();
    }
}

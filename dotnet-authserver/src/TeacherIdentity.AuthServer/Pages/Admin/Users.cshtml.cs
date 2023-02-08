using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Infrastructure.Security;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Pages.Admin;

[Authorize(AuthorizationPolicies.GetAnIdentitySupport)]
public class UsersModel : PageModel
{
    private readonly TeacherIdentityServerDbContext _dbContext;

    public UsersModel(TeacherIdentityServerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [Display(Name = "TRN lookup status")]
    [FromQuery(Name = "LookupStatus")]
    public TrnLookupStatus?[] LookupStatus { get; set; }

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
        var pageSize = 100;

        Expression<Func<User, bool>> filterPredicate = LookupStatus.Length == 0
            ? user => user.UserType == UserType.Default
            : user => user.UserType == UserType.Default && LookupStatus.Contains(user.TrnLookupStatus);

        Expression<Func<User, UserInfo>> mapUserInfo = user => new UserInfo()
        {
            UserId = user.UserId,
            EmailAddress = user.EmailAddress,
            Name = user.FirstName + " " + user.LastName,
            Trn = user.Trn,
            TrnLookupStatus = user.TrnLookupStatus
        };

        var sortedUsers = _dbContext.Users.OrderBy(u => u.LastName).ThenBy(u => u.FirstName);

        FilteredUsers = await sortedUsers.Where(filterPredicate)
            .Skip((PageNumber.Value - 1) * pageSize)
            .Take(pageSize)
            .Select(mapUserInfo)
            .ToArrayAsync();

        TotalUsers = await sortedUsers.Where(filterPredicate).CountAsync();
        TotalPages = (int)Math.Ceiling((decimal)TotalUsers / pageSize);

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
        };

        for (var i = 0; i < LookupStatus.Length; i++)
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
    }
}

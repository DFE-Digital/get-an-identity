using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Pages.Admin;

public class StaffModel : PageModel
{
    private readonly TeacherIdentityServerDbContext _dbContext;

    public StaffModel(TeacherIdentityServerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public StaffUserInfo[]? Users { get; set; }

    public async Task OnGet()
    {
        Users = await _dbContext.Users
            .Where(u => u.UserType == UserType.Staff)
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .Select(u => new StaffUserInfo()
            {
                UserId = u.UserId,
                Email = u.EmailAddress,
                FirstName = u.FirstName,
                LastName = u.LastName
            })
            .ToArrayAsync();
    }

    public class StaffUserInfo
    {
        public required Guid UserId { get; init; }
        public required string Email { get; init; }
        public required string FirstName { get; init; }
        public required string LastName { get; init; }
    }
}

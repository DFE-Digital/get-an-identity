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
        public Guid UserId { get; set; }
        public string Email { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Pages.Admin.MergeUser;

[Authorize(AuthorizationPolicies.GetAnIdentityAdmin)]
public class Confirm : PageModel
{
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IClock _clock;

    public Confirm(TeacherIdentityServerDbContext dbContext, IClock clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    [FromRoute]
    public Guid UserId { get; set; }

    public new User? User { get; set; }

    [FromRoute]
    public Guid UserIdToMerge { get; set; }

    public User? UserToMerge { get; set; }

    [BindProperty]
    public string? ChosenTrn { get; set; }


    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        MergeUsers(User!, UserToMerge!);
        UpdateMergedUser(User!);

        await _dbContext.SaveChangesAsync();

        TempData.SetFlashSuccess("User merged");

        return RedirectToPage("/Admin/User", new { UserId });
    }

    public override async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        User = await GetUser(UserId);
        UserToMerge = await GetUser(UserIdToMerge);

        if (User == null || UserToMerge == null)
        {
            context.Result = NotFound();
            return;
        }

        if (!ValidateChosenTrn())
        {
            context.Result = BadRequest();
            return;
        }

        await next();
    }

    private async Task<User?> GetUser(Guid userId)
    {
        var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.UserId == userId);

        return (user is null || user.UserType != UserType.Default) ? null : user;
    }

    private void MergeUsers(User user, User userToMerge)
    {
        userToMerge.MergedWithUserId = user.UserId;
        userToMerge.IsDeleted = true;

        var previouslyMergedUsers = userToMerge.MergedUsers ?? Enumerable.Empty<User>();
        foreach (var previouslyMergedUser in previouslyMergedUsers)
        {
            previouslyMergedUser.MergedWithUserId = user.UserId;
        }

        _dbContext.AddEvent(new Events.UserMergedEvent()
        {
            User = userToMerge,
            MergedWithUserId = user.UserId,
            CreatedUtc = _clock.UtcNow,
        });
    }

    private void UpdateMergedUser(User user)
    {
        if (user.Trn == ChosenTrn)
        {
            return;
        }

        user.Trn = ChosenTrn;

        _dbContext.AddEvent(new Events.UserUpdatedEvent
        {
            Source = Events.UserUpdatedEventSource.SupportUi,
            CreatedUtc = _clock.UtcNow,
            Changes = Events.UserUpdatedEventChanges.Trn,
            User = user,
            UpdatedByUserId = HttpContext.User.GetUserId(),
            UpdatedByClientId = null
        });
    }

    private bool ValidateChosenTrn()
    {
        if (Request.Query.ContainsKey("trn"))
        {
            ChosenTrn = Request.Query["trn"];
        }

        ChosenTrn = ChosenTrn == "None" ? null : ChosenTrn ?? User!.Trn;
        return ChosenTrn == User!.Trn || ChosenTrn == UserToMerge!.Trn;
    }
}

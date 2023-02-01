using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Pages.Admin.MergeUser;

public class Confirm : PageModel
{
    public static string ChosenTrnKey = "ChosenTrn";
    public string? ChosenTrn;

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

        HttpContext.Session.Remove(ChosenTrnKey);

        TempData[TempDataKeys.FlashSuccess] = "User merged";

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

        if (!TryGetValidChosenTrn(out ChosenTrn))
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
            UpdatedByUserId = HttpContext.User.GetUserId()!.Value,
            UpdatedByClientId = null
        });
    }

    private bool TryGetValidChosenTrn(out string? trn)
    {
        trn = HttpContext.Session.GetString(ChosenTrnKey);
        trn = trn == "None" ? null : trn ?? User!.Trn;

        return trn == User!.Trn || trn == UserToMerge!.Trn;
    }
}

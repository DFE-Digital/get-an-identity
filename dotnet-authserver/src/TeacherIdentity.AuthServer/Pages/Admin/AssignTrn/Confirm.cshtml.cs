using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Infrastructure.Security;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.DqtApi;

namespace TeacherIdentity.AuthServer.Pages.Admin.AssignTrn;

[Authorize(AuthorizationPolicies.GetAnIdentitySupport)]
public class ConfirmModel : PageModel
{
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IDqtApiClient _dqtApiClient;
    private readonly IClock _clock;

    public ConfirmModel(TeacherIdentityServerDbContext dbContext, IDqtApiClient dqtApiClient, IClock clock)
    {
        _dbContext = dbContext;
        _dqtApiClient = dqtApiClient;
        _clock = clock;
    }

    [FromRoute]
    public Guid UserId { get; set; }

    [FromQuery(Name = "trn")]
    public string? Trn { get; set; }

    public string? EmailAddress { get; set; }

    public string? Name { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public string? DqtName { get; set; }

    public DateOnly? DqtDateOfBirth { get; set; }

    public string? DqtEmailAddress { get; set; }

    [BindProperty]
    [Display(Name = "Do you want to assign this TRN?")]
    public bool? AssignTrn { get; set; }

    [BindProperty]
    [Display(Name = "User does not have a TRN")]
    public bool ConfirmNoTrn { get; set; }

    public IActionResult OnGet()
    {
        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        if (Trn is not null && AssignTrn is null)
        {
            ModelState.AddModelError(nameof(AssignTrn), "Tell us if you want to assign this TRN");
        }

        if (Trn is null && !ConfirmNoTrn)
        {
            ModelState.AddModelError(nameof(ConfirmNoTrn), "Confirm the user does not have a TRN");
        }

        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        if (AssignTrn == false)
        {
            return RedirectToPage("/Admin/AssignTrn/Index", new { UserId, hasTrn = true });
        }

        var user = await _dbContext.Users.SingleAsync(u => u.UserId == UserId);

        user.Trn = Trn;
        user.TrnLookupStatus = Trn is not null ? TrnLookupStatus.Found : TrnLookupStatus.Failed;
        user.TrnAssociationSource = TrnAssociationSource.SupportUi;
        user.Updated = _clock.UtcNow;

        var changes = (Trn is not null ? Events.UserUpdatedEventChanges.Trn : Events.UserUpdatedEventChanges.None) |
            Events.UserUpdatedEventChanges.TrnLookupStatus;

        _dbContext.AddEvent(new Events.UserUpdatedEvent()
        {
            Source = Events.UserUpdatedEventSource.SupportUi,
            CreatedUtc = _clock.UtcNow,
            Changes = changes,
            User = Events.User.FromModel(user),
            UpdatedByUserId = User.GetUserId(),
            UpdatedByClientId = null
        });

        await _dbContext.SaveChangesAsync();

        TempData.SetFlashSuccess("TRN assigned");
        return RedirectToPage("/Admin/User", new { UserId });
    }

    public override async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.UserId == UserId);

        if (user is null || user.UserType != UserType.Default)
        {
            context.Result = NotFound();
            return;
        }

        if (user.Trn is not null)
        {
            context.Result = BadRequest();
            return;
        }

        EmailAddress = user.EmailAddress;
        Name = $"{user.FirstName} {user.LastName}";
        DateOfBirth = user.DateOfBirth;

        if (Trn is not null)
        {
            var dqtTeacher = await _dqtApiClient.GetTeacherByTrn(Trn);

            if (dqtTeacher is null)
            {
                context.Result = NotFound();
                return;
            }

            DqtName = $"{dqtTeacher.FirstName} {dqtTeacher.LastName}";
            DqtDateOfBirth = dqtTeacher.DateOfBirth;
            DqtEmailAddress = dqtTeacher.Email;
        }

        await next();
    }
}

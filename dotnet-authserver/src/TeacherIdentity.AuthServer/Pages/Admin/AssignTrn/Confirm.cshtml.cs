using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Helpers;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.DqtApi;

namespace TeacherIdentity.AuthServer.Pages.Admin.AssignTrn;

[Authorize(AuthorizationPolicies.GetAnIdentitySupport)]
public class ConfirmModel : PageModel
{
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IDqtApiClient _dqtApiClient;
    private readonly IClock _clock;

    public ConfirmModel(
        TeacherIdentityServerDbContext dbContext,
        IDqtApiClient dqtApiClient,
        IClock clock)
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

    public string? DqtFirstName { get; set; }

    public string? DqtMiddleName { get; set; }

    public string? DqtLastName { get; set; }

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

        var changes = Events.UserUpdatedEventChanges.TrnLookupStatus;
        user.Trn = Trn;
        user.TrnAssociationSource = TrnAssociationSource.SupportUi;
        user.Updated = _clock.UtcNow;

        if (Trn is not null)
        {
            changes = changes | Events.UserUpdatedEventChanges.Trn;
            user.TrnLookupStatus = TrnLookupStatus.Found;

            changes |= (user.FirstName != DqtFirstName ? Events.UserUpdatedEventChanges.FirstName : Events.UserUpdatedEventChanges.None) |
                           ((user.MiddleName ?? string.Empty) != (DqtMiddleName ?? string.Empty) ? Events.UserUpdatedEventChanges.MiddleName : Events.UserUpdatedEventChanges.None) |
                           (user.LastName != DqtLastName ? Events.UserUpdatedEventChanges.LastName : Events.UserUpdatedEventChanges.None);
            user.FirstName = DqtFirstName!;
            user.MiddleName = DqtMiddleName;
            user.LastName = DqtLastName!;
        }
        else
        {
            user.TrnLookupStatus = TrnLookupStatus.Failed;
        }

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
        Name = NameHelper.GetFullName(user.FirstName, user.MiddleName, user.LastName);
        DateOfBirth = user.DateOfBirth;

        if (Trn is not null)
        {
            var dqtTeacher = await _dqtApiClient.GetTeacherByTrn(Trn);

            if (dqtTeacher is null)
            {
                context.Result = NotFound();
                return;
            }

            DqtFirstName = dqtTeacher.FirstName;
            DqtMiddleName = dqtTeacher.MiddleName;
            DqtLastName = dqtTeacher.LastName;
            DqtDateOfBirth = dqtTeacher.DateOfBirth;
            DqtEmailAddress = dqtTeacher.Email;
        }

        await next();
    }
}

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

    [FromRoute]
    public string Trn { get; set; } = default!;

    public string? EmailAddress { get; set; }

    public string? Name { get; set; }

    public string? DqtName { get; set; }

    public DateOnly? DqtDateOfBirth { get; set; }

    public string? DqtNationalInsuranceNumber { get; set; }

    [BindProperty]
    [Display(Name = "Do you want to add this DQT record?")]
    [Required(ErrorMessage = "Tell us if this is the right DQT record")]
    public bool? AddRecord { get; set; }

    public IActionResult OnGet()
    {
        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        if (AddRecord == false)
        {
            return RedirectToPage("/Admin/User", new { UserId });
        }

        var user = await _dbContext.Users.SingleAsync(u => u.UserId == UserId);

        user.Trn = Trn;
        user.TrnLookupStatus = TrnLookupStatus.Found;
        user.TrnAssociationSource = TrnAssociationSource.SupportUi;
        user.Updated = _clock.UtcNow;

        var changes = Events.UserUpdatedEventChanges.Trn | Events.UserUpdatedEventChanges.TrnLookupStatus;

        _dbContext.AddEvent(new Events.UserUpdatedEvent()
        {
            Source = Events.UserUpdatedEventSource.SupportUi,
            CreatedUtc = _clock.UtcNow,
            Changes = changes,
            User = Events.User.FromModel(user),
            UpdatedByUserId = User.GetUserId()!.Value,
            UpdatedByClientId = null
        });

        await _dbContext.SaveChangesAsync();

        TempData.SetFlashSuccess("DQT record added");
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

        var dqtTeacher = await _dqtApiClient.GetTeacherByTrn(Trn);

        if (dqtTeacher is null)
        {
            context.Result = NotFound();
            return;
        }

        EmailAddress = user.EmailAddress;
        Name = $"{user.FirstName} {user.LastName}";
        DqtName = $"{dqtTeacher.FirstName} {dqtTeacher.LastName}";
        DqtDateOfBirth = dqtTeacher.DateOfBirth;
        DqtNationalInsuranceNumber = dqtTeacher.NationalInsuranceNumber;

        await next();
    }
}

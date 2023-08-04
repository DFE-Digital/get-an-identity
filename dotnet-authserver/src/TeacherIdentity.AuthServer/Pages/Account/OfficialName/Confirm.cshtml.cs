using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Infrastructure.Filters;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.DqtApi;
using TeacherIdentity.AuthServer.Services.DqtEvidence;

namespace TeacherIdentity.AuthServer.Pages.Account.OfficialName;

[VerifyQueryParameterSignature]
[CheckOfficialNameChangeIsEnabled]
public class Confirm : PageModel
{
    private const int SasTokenValidMinutes = 15;
    private readonly IdentityLinkGenerator _linkGenerator;
    private readonly IDqtApiClient _dqtApiClient;
    private readonly IDqtEvidenceStorageService _dqtEvidenceStorage;
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IClock _clock;

    public Confirm(
        IdentityLinkGenerator linkGenerator,
        IDqtApiClient dqtApiClient,
        IDqtEvidenceStorageService dqtEvidenceStorage,
        TeacherIdentityServerDbContext dbContext,
        IClock clock)
    {
        _linkGenerator = linkGenerator;
        _dqtApiClient = dqtApiClient;
        _dqtEvidenceStorage = dqtEvidenceStorage;
        _dbContext = dbContext;
        _clock = clock;
    }

    public ClientRedirectInfo? ClientRedirectInfo => HttpContext.GetClientRedirectInfo();

    [FromQuery(Name = "firstName")]
    public string? FirstName { get; set; }

    [FromQuery(Name = "middleName")]
    public string? MiddleName { get; set; }

    [FromQuery(Name = "lastName")]
    public string? LastName { get; set; }

    [FromQuery(Name = "fileName")]
    public string? FileName { get; set; }

    [FromQuery(Name = "fileId")]
    public string? FileId { get; set; }

    [FromQuery(Name = "preferredName")]
    public string? PreferredName { get; set; }

    public bool HasPreferreredNameChanged { get; set; }

    public async Task OnGet()
    {
        var user = await _dbContext.Users.SingleAsync(u => u.UserId == User.GetUserId());
        HasPreferreredNameChanged = user.PreferredName != PreferredName;
    }

    public async Task<IActionResult> OnPost()
    {
        var sasUri = await _dqtEvidenceStorage.GetSasConnectionString(FileId!, SasTokenValidMinutes);

        var teacherNameChangeRequest = new TeacherNameChangeRequest()
        {
            FirstName = FirstName!,
            MiddleName = MiddleName,
            LastName = LastName!,
            EvidenceFileName = FileName!,
            EvidenceFileUrl = sasUri,
            Trn = User.GetTrn()!
        };

        await _dqtApiClient.PostTeacherNameChange(teacherNameChangeRequest);

        var preferredNameMessage = string.Empty;
        var user = await _dbContext.Users.SingleAsync(u => u.UserId == User.GetUserId());

        if (user.PreferredName != PreferredName)
        {
            user.PreferredName = PreferredName!;
            user.Updated = _clock.UtcNow;

            _dbContext.AddEvent(new UserUpdatedEvent()
            {
                Source = UserUpdatedEventSource.ChangedByUser,
                CreatedUtc = _clock.UtcNow,
                Changes = UserUpdatedEventChanges.PreferredName,
                User = user,
                UpdatedByUserId = User.GetUserId(),
                UpdatedByClientId = null
            });

            await _dbContext.SaveChangesAsync();

            preferredNameMessage = "<br/><br/>Your preferred name has been updated.";
        }

        TempData.SetFlashSuccess(
            "We’ve received your request to change your official name",
            $"We’ll review it and get back to you within 5 working days.{preferredNameMessage}");

        return Redirect(_linkGenerator.Account(ClientRedirectInfo));
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        if (FirstName is null || LastName is null || FileName is null || FileId is null || PreferredName is null)
        {
            context.Result = BadRequest();
        }
    }
}

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Pages.Common;
using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Register;

public class ExistingAccountEmailConfirmation : BaseEmailConfirmationPageModel
{
    private readonly IIdentityLinkGenerator _linkGenerator;
    private readonly TeacherIdentityServerDbContext _dbContext;

    public ExistingAccountEmailConfirmation(
        IUserVerificationService userVerificationService,
        PinValidator pinValidator,
        IIdentityLinkGenerator linkGenerator,
        TeacherIdentityServerDbContext dbContext)
        : base(userVerificationService, pinValidator)
    {
        _linkGenerator = linkGenerator;
        _dbContext = dbContext;
    }

    public override string? Email => HttpContext.GetAuthenticationState().ExistingAccountEmail;
    public new User? User { get; set; }

    [BindProperty]
    [Display(Name = "Confirmation code")]
    public override string? Code { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPost()
    {
        Code = Code?.Trim();
        ValidateCode();

        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        var pinVerificationFailedReasons = await UserVerificationService.VerifyEmailPin(Email!, Code!);

        if (pinVerificationFailedReasons != PinVerificationFailedReasons.None)
        {
            return await HandlePinVerificationFailed(pinVerificationFailedReasons);
        }

        var authenticationState = HttpContext.GetAuthenticationState();

        authenticationState.OnExistingAccountVerified(User!);
        await authenticationState.SignIn(HttpContext);

        return Redirect(authenticationState.GetNextHopUrl(_linkGenerator));
    }

    public override async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        var authenticationState = HttpContext.GetAuthenticationState();

        if (authenticationState.ExistingAccountChosen != true)
        {
            context.Result = new RedirectResult(_linkGenerator.RegisterAccountExists());
            return;
        }

        User = await _dbContext.Users.Where(u => u.UserType == UserType.Default && u.EmailAddress == Email).SingleOrDefaultAsync();

        if (User is null)
        {
            context.Result = NotFound();
            return;
        }

        if (!authenticationState.GetPermittedUserTypes().Contains(User.UserType))
        {
            context.Result = new ForbidResult();
            return;
        }

        await next();
    }
}

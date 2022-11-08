using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Infrastructure.ModelBinding;
using TeacherIdentity.AuthServer.Oidc;

namespace TeacherIdentity.AuthServer.Pages.Admin;

public class AddClientModel : PageModel
{
    private readonly TeacherIdentityApplicationManager _applicationManager;
    private readonly IClock _clock;

    public AddClientModel(
        TeacherIdentityApplicationManager applicationManager,
        IClock clock)
    {
        _applicationManager = applicationManager;
        _clock = clock;
    }

    [BindProperty]
    [Display(Name = "Client ID")]
    [Required(ErrorMessage = "Enter a client ID")]
    public string? ClientId { get; set; }

    [BindProperty]
    [Display(Name = "Client secret")]
    [Required(ErrorMessage = "Enter a client secret")]
    public string? ClientSecret { get; set; }

    [BindProperty]
    [Display(Name = "Display name", Description = "The service name used in the header during the sign in process")]
    [Required(ErrorMessage = "Enter a display name")]
    public string? DisplayName { get; set; }

    [BindProperty]
    [Display(Name = "Service URL", Description = "The link used in the header to go back to the client")]
    public string? ServiceUrl { get; set; }

    [BindProperty]
    [Display(Name = "Redirect URIs", Description = "Enter one per line")]
    [ModelBinder(BinderType = typeof(MultiLineStringModelBinder))]
    public string[]? RedirectUris { get; set; }

    [BindProperty]
    [Display(Name = "Post logout redirect URIs", Description = "Enter one per line")]
    [ModelBinder(BinderType = typeof(MultiLineStringModelBinder))]
    public string[]? PostLogoutRedirectUris { get; set; }

    [BindProperty]
    public string[]? Scopes { get; set; }

    public void OnGet()
    {
        RedirectUris = Array.Empty<string>();
        PostLogoutRedirectUris = Array.Empty<string>();
        Scopes = Array.Empty<string>();
    }

    public async Task<IActionResult> OnPost()
    {
        foreach (var redirectUri in RedirectUris!)
        {
            if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out var uri) ||
                (uri.Scheme != "http" && uri.Scheme != "https"))
            {
                ModelState.AddModelError(nameof(RedirectUris), "One or more redirect URIs are not valid");
                break;
            }
        }

        foreach (var redirectUri in PostLogoutRedirectUris!)
        {
            if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out var uri) ||
                (uri.Scheme != "http" && uri.Scheme != "https"))
            {
                ModelState.AddModelError(nameof(PostLogoutRedirectUris), "One or more redirect URIs are not valid");
                break;
            }
        }

        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        var cleansedScopes = Scopes!.Where(sc => CustomScopes.All.Contains(sc));

        var descriptor = TeacherIdentityApplicationDescriptor.Create(
            ClientId!,
            ClientSecret!,
            DisplayName!,
            ServiceUrl!,
            RedirectUris,
            PostLogoutRedirectUris,
            cleansedScopes);

        var dbContext = _applicationManager.Store.Context;

        using (var txn = await dbContext.Database.BeginTransactionAsync())
        {
            try
            {
                await _applicationManager.CreateAsync(descriptor);
            }
            catch (OpenIddict.Abstractions.OpenIddictExceptions.ValidationException ex) when (ex.Message.Contains("An application with the same client identifier already exists."))
            {
                ModelState.AddModelError(nameof(ClientId), "A client already exists with the specified client ID");
                return this.PageWithErrors();
            }

            dbContext.AddEvent(new ClientAddedEvent()
            {
                AddedByUserId = User.GetUserId()!.Value,
                Client = Client.FromDescriptor(descriptor),
                CreatedUtc = _clock.UtcNow
            });

            await dbContext.SaveChangesAsync();

            await txn.CommitAsync();
        }

        TempData.SetFlashSuccess("Client added");
        return RedirectToPage("Clients");
    }
}
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenIddict.Abstractions;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Infrastructure.ModelBinding;
using TeacherIdentity.AuthServer.Infrastructure.Security;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;

namespace TeacherIdentity.AuthServer.Pages.Admin;

[Authorize(AuthorizationPolicies.GetAnIdentityAdmin)]
[BindProperties]
public class EditClientModel : PageModel
{
    private readonly TeacherIdentityApplicationStore _applicationStore;
    private readonly IClock _clock;

    public EditClientModel(TeacherIdentityApplicationStore applicationStore, IClock clock)
    {
        _applicationStore = applicationStore;
        _clock = clock;
    }

    [FromRoute]
    public string ClientId { get; set; } = null!;

    [Display(Name = "Reset client secret")]
    public bool ResetClientSecret { get; set; }

    [Display(Name = "Client secret", Description = "This secret is hashed before it is stored and cannot be retrieved later")]
    public string? ClientSecret { get; set; }

    [Display(Name = "Display name", Description = "The service name used in the header during the sign in process")]
    [Required(ErrorMessage = "Enter a display name")]
    public string? DisplayName { get; set; }

    [Display(Name = "Service URL", Description = "The link used in the header to go back to the client")]
    public string? ServiceUrl { get; set; }

    [Display(Name = "TRN required")]
    public TrnRequirementType? TrnRequired { get; set; }

    public bool EnableAuthorizationCodeFlow { get; set; }

    public bool EnableClientCredentialsFlow { get; set; }

    [Display(Name = "Redirect URIs", Description = "Enter one per line")]
    [ModelBinder(BinderType = typeof(MultiLineStringModelBinder))]
    public string[]? RedirectUris { get; set; }

    [Display(Name = "Post logout redirect URIs", Description = "Enter one per line")]
    [ModelBinder(BinderType = typeof(MultiLineStringModelBinder))]
    public string[]? PostLogoutRedirectUris { get; set; }

    public string[]? Scopes { get; set; }

    public async Task<IActionResult> OnGet()
    {
        var client = await _applicationStore.FindByClientIdAsync(ClientId, CancellationToken.None);
        if (client is null)
        {
            return NotFound();
        }

        DisplayName = client.DisplayName;
        ServiceUrl = client.ServiceUrl;
        TrnRequired = client.TrnRequirementType;
        EnableAuthorizationCodeFlow = client.GetPermissions().Contains(OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode);
        EnableClientCredentialsFlow = client.GetPermissions().Contains(OpenIddictConstants.Permissions.GrantTypes.ClientCredentials);
        RedirectUris = client.GetRedirectUris();
        PostLogoutRedirectUris = client.GetPostLogoutRedirectUris();
        Scopes = client.GetScopes();

        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        var client = await _applicationStore.FindByClientIdAsync(ClientId, CancellationToken.None);
        if (client is null)
        {
            return NotFound();
        }

        if (!EnableAuthorizationCodeFlow)
        {
            RedirectUris = Array.Empty<string>();
            PostLogoutRedirectUris = Array.Empty<string>();
        }

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

        if (ResetClientSecret && string.IsNullOrEmpty(ClientSecret))
        {
            ModelState.AddModelError(nameof(ClientSecret), "Enter a client secret");
        }

        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        var allScopes = Scopes!.Where(sc => CustomScopes.All.Contains(sc))
            .Concat(TeacherIdentityApplicationDescriptor.StandardScopes);

        var grantTypes = new[]
        {
            (GrantType: OpenIddictConstants.GrantTypes.AuthorizationCode, Enabled: EnableAuthorizationCodeFlow),
            (GrantType: OpenIddictConstants.GrantTypes.ClientCredentials, Enabled: EnableClientCredentialsFlow),
        }
            .Where(t => t.Enabled)
            .Select(t => t.GrantType)
            .ToArray();

        var permissions = client.GetPermissions()
            .Where(p => !p.StartsWith(OpenIddictConstants.Permissions.Prefixes.Scope))
            .Where(p => !p.StartsWith(OpenIddictConstants.Permissions.Prefixes.GrantType))
            .Concat(allScopes.Select(s => OpenIddictConstants.Permissions.Prefixes.Scope + s))
            .Concat(grantTypes.Select(t => OpenIddictConstants.Permissions.Prefixes.GrantType + t))
            .ToImmutableArray();

        var changes = ClientUpdatedEventChanges.None |
            (ResetClientSecret ? ClientUpdatedEventChanges.ClientSecret : ClientUpdatedEventChanges.None) |
            (DisplayName != client.DisplayName ? ClientUpdatedEventChanges.DisplayName : ClientUpdatedEventChanges.None) |
            (ServiceUrl != client.ServiceUrl ? ClientUpdatedEventChanges.ServiceUrl : ClientUpdatedEventChanges.None) |
            (!RedirectUris.SequenceEqualIgnoringOrder(client.GetRedirectUris()) ? ClientUpdatedEventChanges.RedirectUris : ClientUpdatedEventChanges.None) |
            (!PostLogoutRedirectUris.SequenceEqualIgnoringOrder(client.GetPostLogoutRedirectUris()) ? ClientUpdatedEventChanges.PostLogoutRedirectUris : ClientUpdatedEventChanges.None) |
            (!allScopes.SequenceEqualIgnoringOrder(client.GetScopes()) ? ClientUpdatedEventChanges.Scopes : ClientUpdatedEventChanges.None) |
            (!grantTypes.SequenceEqualIgnoringOrder(client.GetGrantTypes()) ? ClientUpdatedEventChanges.GrantTypes : ClientUpdatedEventChanges.None);

        await _applicationStore.SetDisplayNameAsync(client, DisplayName, CancellationToken.None);
        await _applicationStore.SetServiceUrlAsync(client, ServiceUrl);
        await _applicationStore.SetRedirectUrisAsync(client, RedirectUris.ToImmutableArray(), CancellationToken.None);
        await _applicationStore.SetPostLogoutRedirectUrisAsync(client, PostLogoutRedirectUris.ToImmutableArray(), CancellationToken.None);
        await _applicationStore.SetPermissionsAsync(client, permissions, CancellationToken.None);

        if (TrnRequired is not null && TrnRequired != client.TrnRequirementType)
        {
            changes |= ClientUpdatedEventChanges.TrnRequirementType;
            await _applicationStore.SetTrnRequirementTypeAsync(client, (TrnRequirementType)TrnRequired);
        }

        if (!string.IsNullOrEmpty(ClientSecret))
        {
            await _applicationStore.SetClientSecretAsync(client, ClientSecret, CancellationToken.None);
        }

        if (changes != ClientUpdatedEventChanges.None)
        {
            var dbContext = _applicationStore.Context;

            using (var txn = await dbContext.Database.BeginTransactionAsync())
            {
                await _applicationStore.UpdateAsync(client, CancellationToken.None);

                dbContext.AddEvent(new ClientUpdatedEvent()
                {
                    UpdatedByUserId = User.GetUserId()!.Value,
                    Client = client,
                    CreatedUtc = _clock.UtcNow,
                    Changes = changes
                });

                await dbContext.SaveChangesAsync();

                await txn.CommitAsync();
            }

            TempData.SetFlashSuccess(new FlashSuccessData("Client updated"));
        }

        return RedirectToPage("Clients");
    }
}

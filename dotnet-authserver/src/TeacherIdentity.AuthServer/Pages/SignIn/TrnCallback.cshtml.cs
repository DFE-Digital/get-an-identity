using System.Security.Claims;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Models;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TeacherIdentity.AuthServer.Pages.SignIn
{
    public class TrnCallbackModel : PageModel
    {
        private readonly FindALostTrnIntegrationHelper _findALostTrnIntegrationHelper;
        private readonly TeacherIdentityServerDbContext _dbContext;

        public TrnCallbackModel(
            FindALostTrnIntegrationHelper findALostTrnIntegrationHelper,
            TeacherIdentityServerDbContext dbContext)
        {
            _findALostTrnIntegrationHelper = findALostTrnIntegrationHelper;
            _dbContext = dbContext;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var authenticationState = HttpContext.GetAuthenticationState();

            var requestUrl = UriHelper.GetEncodedUrl(Request);
            if (!_findALostTrnIntegrationHelper.ValidateCallback(requestUrl, out var findALostTrnUser))
            {
                throw new Exception("Invalid callback from Find a lost TRN service.");
            }

            // We don't expect to have an existing user at this point
            if (authenticationState.UserId.HasValue)
            {
                throw new NotImplementedException();
            }

            // TODO Do we propagate email address from Find?

            CheckClaimsProvided(findALostTrnUser, "trn", Claims.GivenName, Claims.FamilyName);
            authenticationState.Trn = findALostTrnUser.FindFirst("trn")!.Value;
            authenticationState.FirstName = findALostTrnUser.FindFirst(Claims.GivenName)!.Value;
            authenticationState.LastName = findALostTrnUser.FindFirst(Claims.FamilyName)!.Value;

            var userId = Guid.NewGuid();
            var user = new TeacherIdentityUser()
            {
                EmailAddress = authenticationState.EmailAddress,
                FirstName = authenticationState.FirstName,
                LastName = authenticationState.LastName,
                Trn = authenticationState.Trn,
                UserId = userId
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            authenticationState.UserId = userId;

            return await HttpContext.SignInUser(user);

            static void CheckClaimsProvided(ClaimsPrincipal principal, params string[] claimTypes)
            {
                foreach (var claimType in claimTypes)
                {
                    if (!principal.HasClaim(c => c.Type == claimType))
                    {
                        throw new Exception($"Required claim '{claimType}' was not provided.");
                    }
                }
            }
        }
    }
}

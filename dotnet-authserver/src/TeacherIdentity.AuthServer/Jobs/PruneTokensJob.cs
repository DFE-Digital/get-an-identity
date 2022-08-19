using OpenIddict.Abstractions;

namespace TeacherIdentity.AuthServer.Jobs;

// A much-simplified version of the Quartz implementation at https://github.com/openiddict/openiddict-core/blob/3.1.1/src/OpenIddict.Quartz/OpenIddictQuartzJob.cs
public class PruneTokensJob
{
    private static readonly TimeSpan _minimumTokenLifespan = TimeSpan.FromDays(14);
    private static readonly TimeSpan _minimumAuthorizationLifespan = TimeSpan.FromDays(14);

    private readonly IOpenIddictTokenManager _tokenManager;
    private readonly IOpenIddictAuthorizationManager _authorizationManager;

    public PruneTokensJob(
        IOpenIddictTokenManager tokenManager,
        IOpenIddictAuthorizationManager authorizationManager)
    {
        _tokenManager = tokenManager;
        _authorizationManager = authorizationManager;
    }

    public async Task Execute(CancellationToken cancellationToken)
    {
        {
            var threshold = DateTimeOffset.UtcNow - _minimumTokenLifespan;
            await _tokenManager.PruneAsync(threshold, cancellationToken);
        }

        {
            var threshold = DateTimeOffset.UtcNow - _minimumAuthorizationLifespan;
            await _authorizationManager.PruneAsync(threshold, cancellationToken);
        }
    }
}

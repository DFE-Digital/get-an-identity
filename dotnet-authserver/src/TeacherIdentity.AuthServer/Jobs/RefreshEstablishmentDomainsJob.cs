using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.Establishment;

namespace TeacherIdentity.AuthServer.Jobs;

public class RefreshEstablishmentDomainsJob
{
    private const string ExtractDomainRegex = "^(?:https?:\\/\\/)?(?:[^@\\/\\n]+@)?(?:www\\.)?([^:\\/\\n]+)";
    private string[] additionalKnownEstablishmentDomains = new[] { "leghvale.st-helens.sch.uk", "mansfieldgreenacademy.e-act.org.uk" };
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IEstablishmentMasterDataService _establishmentMasterDataService;
    private readonly ILogger<RefreshEstablishmentDomainsJob> _logger;

    public RefreshEstablishmentDomainsJob(
        TeacherIdentityServerDbContext dbContext,
        IEstablishmentMasterDataService establishmentMasterDataService,
        ILogger<RefreshEstablishmentDomainsJob> logger)
    {
        _dbContext = dbContext;
        _establishmentMasterDataService = establishmentMasterDataService;
        _logger = logger;
    }

    public async Task Execute(CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var existingDomains = await _dbContext.EstablishmentDomains.AsNoTracking().Select(d => d.DomainName).ToListAsync();

        int i = 0;
        await foreach (var website in _establishmentMasterDataService.GetEstablishmentWebsites())
        {
            if (!string.IsNullOrWhiteSpace(website))
            {
                if (!Uri.IsWellFormedUriString(website, UriKind.RelativeOrAbsolute))
                {
                    _logger.LogWarning("School/establishment website {website} is not a valid URI", website);
                    continue;
                }

                var match = Regex.Match(website, ExtractDomainRegex);
                var domainName = match.Groups.Values.Last().Value;
                if (Uri.CheckHostName(domainName) == UriHostNameType.Unknown)
                {
                    _logger.LogWarning("{domainName} is not a valid domain name", domainName);
                    continue;
                }

                if (TryAddEstablishmentDomain(domainName))
                {
                    if (i != 0 && i % 2_000 == 0)
                    {
                        await SaveChangesAndLog(cancellationToken);
                    }

                    i++;
                }
            }
        }

        foreach (var knownDomainName in additionalKnownEstablishmentDomains)
        {
            if (TryAddEstablishmentDomain(knownDomainName))
            {
                i++;
            }
        }

        if (_dbContext.ChangeTracker.HasChanges())
        {
            await SaveChangesAndLog(cancellationToken);
        }

        sw.Stop();
        _logger.LogInformation($"Refresh establishment domain job completed in {sw.ElapsedMilliseconds}ms.");

        bool TryAddEstablishmentDomain(string domainName)
        {
            if (existingDomains!.Contains(domainName))
            {
                return false;
            }
            else
            {
                var establishmentDomain = new EstablishmentDomain
                {
                    DomainName = domainName
                };

                _dbContext.EstablishmentDomains.Add(establishmentDomain);
                existingDomains.Add(domainName);
                return true;
            }
        }

        async Task SaveChangesAndLog(CancellationToken cancellationToken)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Saved {i} establishment domains in database.", i);
        }
    }
}

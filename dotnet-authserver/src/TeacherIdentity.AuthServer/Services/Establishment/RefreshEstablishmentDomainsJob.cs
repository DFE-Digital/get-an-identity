using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Services.Establishment;

public class RefreshEstablishmentDomainsJob
{
    private const string ExtractDomainRegex = "^(?:https?:\\/\\/)?(?:[^@\\/\\n]+@)?(?:www\\.)?([^:\\/\\n]+)";
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IEstablishmentMasterDataService _establishmentMasterDataService;

    public RefreshEstablishmentDomainsJob(
        TeacherIdentityServerDbContext dbContext,
        IEstablishmentMasterDataService establishmentMasterDataService)
    {
        _dbContext = dbContext;
        _establishmentMasterDataService = establishmentMasterDataService;
    }

    public async Task Execute(CancellationToken cancellationToken)
    {
        var existingDomains = await _dbContext.EstablishmentDomains.AsNoTracking().Select(d => d.DomainName).ToListAsync();

        int i = 0;
        await foreach (var website in _establishmentMasterDataService.GetEstablishmentWebsites())
        {
            if (!string.IsNullOrWhiteSpace(website))
            {
                var domainName = Regex.Match(website, ExtractDomainRegex).Value;
                if (!existingDomains.Contains(domainName))
                {
                    var establishmentDomain = new EstablishmentDomain
                    {
                        DomainName = domainName
                    };

                    _dbContext.EstablishmentDomains.Add(establishmentDomain);
                    existingDomains.Add(domainName);

                    if (i != 0 && i % 2_000 == 0)
                    {
                        await _dbContext.SaveChangesAsync(cancellationToken);
                    }

                    i++;
                }
            }
        }

        if (_dbContext.ChangeTracker.HasChanges())
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}

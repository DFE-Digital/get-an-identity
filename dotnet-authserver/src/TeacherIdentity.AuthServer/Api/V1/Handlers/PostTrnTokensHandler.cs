using System.Security.Cryptography;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Api.V1.Requests;
using TeacherIdentity.AuthServer.Api.V1.Responses;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Api.V1.Handlers;

public class PostTrnTokensHandler : IRequestHandler<PostTrnTokensRequest, PostTrnTokenResponse>
{
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IClock _clock;

    public PostTrnTokensHandler(
        TeacherIdentityServerDbContext dbContext,
        IClock clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    public async Task<PostTrnTokenResponse> Handle(PostTrnTokensRequest request, CancellationToken cancellationToken)
    {
        string trnToken;
        do
        {
            var buffer = new byte[64];
            RandomNumberGenerator.Fill(buffer);
            trnToken = Convert.ToHexString(buffer);
        } while (await _dbContext.TrnTokens.AnyAsync(t => t.TrnToken == trnToken, cancellationToken));

        var created = _clock.UtcNow;
        var expires = created.AddMonths(6);

        _dbContext.TrnTokens.Add(new TrnTokenModel()
        {
            TrnToken = trnToken,
            Trn = request.Trn,
            Email = request.Email,
            CreatedUtc = created,
            ExpiresUtc = expires,
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new PostTrnTokenResponse()
        {
            Trn = request.Trn,
            Email = request.Email,
            TrnToken = trnToken,
            ExpiresUtc = expires,
        };
    }
}

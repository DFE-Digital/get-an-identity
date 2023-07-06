using MediatR;
using TeacherIdentity.AuthServer.Api.V1.Requests;
using TeacherIdentity.AuthServer.Api.V1.Responses;
using TeacherIdentity.AuthServer.Services.TrnTokens;

namespace TeacherIdentity.AuthServer.Api.V1.Handlers;

public class CreateTrnTokenHandler : IRequestHandler<CreateTrnTokenRequest, CreateTrnTokenResponse>
{
    private readonly TrnTokenService _trnTokenService;
    private readonly ICurrentUserProvider _currentUserProvider;

    public CreateTrnTokenHandler(TrnTokenService trnTokenService, ICurrentUserProvider currentUserProvider)
    {
        _trnTokenService = trnTokenService;
        _currentUserProvider = currentUserProvider;
    }

    public async Task<CreateTrnTokenResponse> Handle(CreateTrnTokenRequest request, CancellationToken cancellationToken)
    {
        var trnToken = await _trnTokenService.GenerateToken(
            request.Email,
            request.Trn,
            apiClientId: _currentUserProvider.CurrentClientId,
            currentUserId: null);

        return new CreateTrnTokenResponse()
        {
            Trn = request.Trn,
            Email = request.Email,
            TrnToken = trnToken.TrnToken,
            ExpiresUtc = trnToken.ExpiresUtc,
        };
    }
}

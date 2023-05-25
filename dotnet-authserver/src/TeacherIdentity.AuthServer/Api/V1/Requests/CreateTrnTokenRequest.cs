using MediatR;
using TeacherIdentity.AuthServer.Api.V1.Responses;

namespace TeacherIdentity.AuthServer.Api.V1.Requests;

public record CreateTrnTokenRequest : IRequest<CreateTrnTokenResponse>
{
    public required string Trn { get; init; }

    public required string Email { get; init; }
}

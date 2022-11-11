using MediatR;
using TeacherIdentity.AuthServer.Api.V1.Responses;

namespace TeacherIdentity.AuthServer.Api.V1.Requests;

public record GetUserDetailRequest : IRequest<GetUserDetailResponse>
{
    public required Guid UserId { get; init; }
}

using MediatR;
using TeacherIdentity.AuthServer.Api.V1.Responses;

namespace TeacherIdentity.AuthServer.Api.V1.Requests;

public class GetUserDetailRequest : IRequest<GetUserDetailResponse>
{
    public required Guid UserId { get; init; }
}

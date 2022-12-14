using MediatR;
using TeacherIdentity.AuthServer.Api.V1.Responses;

namespace TeacherIdentity.AuthServer.Api.V1.Requests;

public record GetAllUsersRequest : IRequest<GetAllUsersResponse>
{
    public int? PageNumber { get; set; }
    public int? PageSize { get; set; }
}

using TeacherIdentity.AuthServer.Api.V1.ApiModels;

namespace TeacherIdentity.AuthServer.Api.V1.Responses;

public record GetAllUsersResponse
{
    public required UserInfo[] Users { get; init; }
}

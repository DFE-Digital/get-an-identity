using TeacherIdentity.AuthServer.Api.V1.ApiModels;

namespace TeacherIdentity.AuthServer.Api.V1.Responses;

public class GetAllUsersResponse
{
    public UserInfo[]? Users { get; set; }
}
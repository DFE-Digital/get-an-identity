using TeacherIdentity.AuthServer.Api.V1.ApiModels;

namespace TeacherIdentity.AuthServer.Api.V1.Responses;

public class GetTeachersResponse
{
    public TeacherInfo[]? Teachers { get; set; }
}

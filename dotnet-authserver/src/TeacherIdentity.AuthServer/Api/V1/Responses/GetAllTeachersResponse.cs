using TeacherIdentity.AuthServer.Api.V1.ApiModels;

namespace TeacherIdentity.AuthServer.Api.V1.Responses;

public class GetAllTeachersResponse
{
    public TeacherInfo[]? Teachers { get; set; }
}

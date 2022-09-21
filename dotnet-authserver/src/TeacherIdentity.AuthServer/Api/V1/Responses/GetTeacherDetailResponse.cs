using Swashbuckle.AspNetCore.Filters;
using TeacherIdentity.AuthServer.Api.V1.ApiModels;

namespace TeacherIdentity.AuthServer.Api.V1.Responses;

public class GetTeacherDetailResponse : UserInfo
{
    public class TrnRequestInfoExample : IExamplesProvider<GetTeacherDetailResponse>
    {
        public GetTeacherDetailResponse GetExamples() => new()
        {
            UserId = new Guid("29e9e624-073e-41f5-b1b3-8164ce3a5233"),
            DateOfBirth = new DateOnly(1990, 1, 1),
            Email = "kevin.e@example.com",
            FirstName = "Kevin",
            LastName = "E",
            Trn = "2921020"
        };
    }
}

using Swashbuckle.AspNetCore.Filters;
using TeacherIdentity.AuthServer.Api.V1.ApiModels;

namespace TeacherIdentity.AuthServer.Api.V1.Responses;

public record GetUserDetailResponse : UserInfo
{
    public required DateTime Created { get; init; }
    public required string? RegisteredWithClientId { get; init; }
    public required string? RegisteredWithClientDisplayName { get; init; }
    public required IEnumerable<Guid> MergedUserIds { get; init; }

    public class TrnRequestInfoExample : IExamplesProvider<GetUserDetailResponse>
    {
        public GetUserDetailResponse GetExamples() => new()
        {
            UserId = new Guid("29e9e624-073e-41f5-b1b3-8164ce3a5233"),
            DateOfBirth = new DateOnly(1990, 1, 1),
            Email = "kevin.e@example.com",
            FirstName = "Kevin",
            LastName = "E",
            Trn = "2921020",
            TrnLookupStatus = AuthServer.TrnLookupStatus.Found,
            MobileNumber = "07890123456",
            Created = new DateTime(2022, 11, 2, 16, 19, 0),
            RegisteredWithClientId = "register-for-npq",
            RegisteredWithClientDisplayName = "Register for a National Professional Qualification",
            MergedUserIds = Enumerable.Empty<Guid>()
        };
    }
}

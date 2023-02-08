using TeacherIdentity.AuthServer.Services.DqtApi;
using TeacherIdentity.AuthServer.Tests.Infrastructure;

namespace TeacherIdentity.AuthServer.Tests;

public abstract partial class TestBase
{
    protected TestBase(HostFixture hostFixture)
    {
        HostFixture = hostFixture;

        HttpClient = hostFixture.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions()
        {
            AllowAutoRedirect = false
        });

        HostFixture.ResetMocks();
        HostFixture.ResetUseNewTrnLookupJourney();
        HostFixture.InitEventObserver();
    }

    public TestClock Clock => (TestClock)HostFixture.Services.GetRequiredService<IClock>();

    public CaptureEventObserver EventObserver => HostFixture.EventObserver;

    public HostFixture HostFixture { get; }

    public HttpClient HttpClient { get; }

    public TestData TestData => HostFixture.Services.GetRequiredService<TestData>();

    public Task<AuthenticationStateHelper> CreateAuthenticationStateHelper(
        Func<AuthenticationStateHelper.Configure, Func<AuthenticationState, Task>> configure,
        string? additionalScopes) =>
        AuthenticationStateHelper.Create(configure, HostFixture, additionalScopes);

    public void ConfigureDqtApiClientToReturnSingleMatch(AuthenticationStateHelper authStateHelper)
    {
        var authState = authStateHelper.AuthenticationState;
        var matchedTrn = TestData.GenerateTrn();

        HostFixture.DqtApiClient
            .Setup(mock => mock.FindTeachers(It.Is<FindTeachersRequest>(req =>
                    req.DateOfBirth == authState.DateOfBirth &&
                    req.EmailAddress == authState.EmailAddress &&
                    req.FirstName == authState.OfficialFirstName &&
                    req.LastName == authState.OfficialLastName &&
                    req.NationalInsuranceNumber == authState.NationalInsuranceNumber &&
                    req.PreviousFirstName == authState.PreviousOfficialFirstName &&
                    req.PreviousLastName == authState.PreviousOfficialLastName &&
                    req.IttProviderName == authState.IttProviderName),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FindTeachersResponse()
            {
                Results = new[]
                {
                    new FindTeachersResponseResult()
                    {
                        DateOfBirth = authState.DateOfBirth,
                        EmailAddresses = new[] { authState.EmailAddress! },
                        FirstName = authState.FirstName!,
                        LastName = authState.LastName!,
                        HasActiveSanctions = false,
                        NationalInsuranceNumber = authState.NationalInsuranceNumber,
                        Trn = matchedTrn,
                        Uid = Guid.NewGuid().ToString()
                    }
                }
            });
    }

    public void VerifyDqtApiFindTeachersNotCalled() =>
        HostFixture.DqtApiClient.Verify(mock => mock.FindTeachers(It.IsAny<FindTeachersRequest>(), It.IsAny<CancellationToken>()), Times.Never());
}

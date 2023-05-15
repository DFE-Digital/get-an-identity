using AngleSharp.Html.Dom;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Oidc;
using ZendeskApi.Client.Models;
using ZendeskApi.Client.Requests;
using ZendeskApi.Client.Responses;
using User = TeacherIdentity.AuthServer.Models.User;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn.Register;

public class CheckAnswersTests : TestBase
{
    public CheckAnswersTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, "/sign-in/register/check-answers");
    }

    [Fact]
    public async Task Get_MissingAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, "/sign-in/register/check-answers");
    }

    [Fact]
    public async Task Get_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(CustomScopes.DqtRead, trnRequirementType: null, HttpMethod.Get, "/sign-in/register/check-answers");
    }

    [Fact]
    public async Task Get_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(_currentPageAuthenticationState(), CustomScopes.DqtRead, trnRequirementType: null, HttpMethod.Get, "/sign-in/register/check-answers");
    }

    [Theory]
    [MemberData(nameof(CheckAnswersState))]
    public async Task Get_ValidRequest_RendersExpectedContent(
        bool requiresTrnLookup,
        RegisterJourneyPage registerJourneyStage,
        bool awardedQts)
    {
        // Arrange
        var additionalScopes = requiresTrnLookup ? CustomScopes.DqtRead : null;

        var currentAuthenticationStateConfig = RegisterJourneyAuthenticationStateHelper.ConfigureAuthenticationStateForPage(registerJourneyStage);

        var authStateHelper = await CreateAuthenticationStateHelper(currentAuthenticationStateConfig(awardedQts: awardedQts), additionalScopes);
        var authState = authStateHelper.AuthenticationState;

        if (requiresTrnLookup)
        {
            authState.OnTrnLookupCompleted(TestData.GenerateTrn(), TrnLookupStatus.Found);
        }

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/register/check-answers?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        var doc = await response.GetDocument();
        Assert.Equal(authState.EmailAddress, doc.GetSummaryListValueForKey("Email"));
        Assert.Equal(authState.MobileNumber, doc.GetSummaryListValueForKey("Mobile phone"));
        Assert.Equal($"{authState.FirstName} {authState.LastName}", doc.GetSummaryListValueForKey("Name"));
        Assert.Equal(authState.DateOfBirth?.ToString(Constants.DateFormat), doc.GetSummaryListValueForKey("Date of birth"));

        var hasNiNumberSet = registerJourneyStage > RegisterJourneyPage.HasNiNumber;
        var awardedQtsSet = registerJourneyStage > RegisterJourneyPage.HasQts;

        AssertRowValid("National Insurance number", requiresTrnLookup && hasNiNumberSet, authState.NationalInsuranceNumber, doc);
        AssertRowValid("Have you been awarded QTS?", requiresTrnLookup && awardedQtsSet, authState.AwardedQts == true ? "Yes" : "No", doc);
        AssertRowValid("Where did you get your QTS?", requiresTrnLookup && awardedQts, authState.IttProviderName, doc);
    }

    [Fact]
    public async Task Post_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Post, "/sign-in/register/check-answers");
    }

    [Fact]
    public async Task Post_MissingAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Post, "/sign-in/register/check-answers");
    }

    [Fact]
    public async Task Post_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(CustomScopes.DqtRead, trnRequirementType: null, HttpMethod.Post, "/sign-in/register/check-answers");
    }

    [Fact]
    public async Task Post_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(_currentPageAuthenticationState(), CustomScopes.DqtRead, trnRequirementType: null, HttpMethod.Post, "/sign-in/register/check-answers");
    }

    [Theory]
    [InlineData(true, TrnLookupStatus.Pending, true, true)]
    [InlineData(true, TrnLookupStatus.Pending, false, false)]
    [InlineData(true, TrnLookupStatus.Found, true, false)]
    [InlineData(true, TrnLookupStatus.Found, false, false)]
    [InlineData(false, TrnLookupStatus.None, true, false)]
    [InlineData(false, TrnLookupStatus.None, false, false)]
    public async Task Post_ValidForm_CreatesUserAndRedirectsToPostSignInUrl(
        bool requiresTrnLookup,
        TrnLookupStatus trnLookupStatus,
        bool raiseTrnResolutionSupportTickets,
        bool expectZendeskTicketCreated)
    {
        // Arrange
        var additionalScopes = requiresTrnLookup ? CustomScopes.DqtRead : null;
        var client = raiseTrnResolutionSupportTickets ? TestClients.RaiseTrnResolutionSupportTickets : TestClients.DefaultClient;

        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(), additionalScopes, client: client);
        var authState = authStateHelper.AuthenticationState;

        if (requiresTrnLookup)
        {
            var trn = trnLookupStatus == TrnLookupStatus.Found ? TestData.GenerateTrn() : null;
            authState.OnTrnLookupCompleted(trn, trnLookupStatus);
        }

        TicketCreateRequest? ticketCreateRequestActual = null;
        long ticketIdExpected = 1234567;
        if (expectZendeskTicketCreated)
        {
            HostFixture.ZendeskApiWrapper
                .Setup(z => z.CreateTicketAsync(It.IsAny<TicketCreateRequest>(), It.IsAny<CancellationToken>()))
                .Callback<TicketCreateRequest, CancellationToken>((r, t) => ticketCreateRequestActual = r)
                .ReturnsAsync(new TicketResponse() { Ticket = new Ticket() { Id = ticketIdExpected } });
        }

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/check-answers?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith(authState.PostSignInUrl, response.Headers.Location?.OriginalString);

        User? user = null;
        await TestData.WithDbContext(async dbContext =>
        {
            user = await dbContext.Users.Where(u => u.EmailAddress == authState.EmailAddress).SingleOrDefaultAsync();
            Assert.NotNull(user);
        });

        var elementInspectors = new List<Action<EventBase>>()
        {
            e =>
            {
                var userRegisteredEvent = Assert.IsType<UserRegisteredEvent>(e);
                Assert.Equal(Clock.UtcNow, userRegisteredEvent.CreatedUtc);
                Assert.Equal(user?.UserId, userRegisteredEvent.User.UserId);
            }
        };

        if (expectZendeskTicketCreated)
        {
            elementInspectors.Add(
                e =>
                {
                    var supportTicketCreatedEvent = Assert.IsType<TrnLookupSupportTicketCreatedEvent>(e);
                    Assert.Equal(ticketIdExpected, supportTicketCreatedEvent.TicketId);
                    Assert.Equal(Clock.UtcNow, supportTicketCreatedEvent.CreatedUtc);
                    Assert.Equal(user?.UserId, supportTicketCreatedEvent.UserId);
                });
        }

        EventObserver.AssertEventsSaved(elementInspectors.ToArray());

        HostFixture.ZendeskApiWrapper.Verify(
            mock => mock.CreateTicketAsync(It.Is<TicketCreateRequest>(t => t.Requester.Email == authState.EmailAddress), It.IsAny<CancellationToken>()),
            expectZendeskTicketCreated ? Times.Once() : Times.Never());

        if (expectZendeskTicketCreated)
        {
            Assert.NotNull(ticketCreateRequestActual);
        }
    }

    private void AssertRowValid(string rowName, bool shouldExist, string? value, IHtmlDocument doc)
    {
        if (shouldExist)
        {
            Assert.Equal(value, doc.GetSummaryListValueForKey(rowName));
        }
        else
        {
            Assert.Null(doc.GetSummaryListRowForKey(rowName));
        }
    }

    public static TheoryData<bool, RegisterJourneyPage, bool> CheckAnswersState { get; } = new()
    {
        // requiresTrnLookup, register journey stage, AwardedQts
        { false, RegisterJourneyPage.CheckAnswers, false },
        { true, RegisterJourneyPage.CheckAnswers, false },
        { true, RegisterJourneyPage.CheckAnswers, true },
        { true, RegisterJourneyPage.HasQts, false },
        { true, RegisterJourneyPage.HasNiNumber, false },
    };

    private readonly AuthenticationStateConfigGenerator _currentPageAuthenticationState = RegisterJourneyAuthenticationStateHelper.ConfigureAuthenticationStateForPage(RegisterJourneyPage.CheckAnswers);
}

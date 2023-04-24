using TeacherIdentity.AuthServer.Oidc;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn;

public class CompleteTests : TestBase
{
    public CompleteTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, "/sign-in/complete");
    }

    [Fact]
    public async Task Get_MissingAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, "/sign-in/complete");
    }

    [Fact]
    public async Task Get_JourneyIsAlreadyCompleted_DoesNotRedirectToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_DoesNotRedirectToPostSignInUrl(additionalScopes: null, HttpMethod.Get, "/sign-in/complete");
    }

    [Fact]
    public async Task Get_JourneyHasExpired_RendersErrorPage()
    {
        var user = await TestData.CreateUser(hasTrn: true);
        await JourneyHasExpired_RendersErrorPage(c => c.Completed(user), additionalScopes: null, HttpMethod.Get, "/sign-in/complete");
    }

    [Theory]
    [IncompleteAuthenticationMilestonesData()]
    public async Task Get_JourneyMilestoneHasPassed_RedirectsToStartOfNextMilestone(
        AuthenticationState.AuthenticationMilestone milestone)
    {
        await JourneyMilestoneHasPassed_RedirectsToStartOfNextMilestone(milestone, HttpMethod.Get, "/sign-in/complete");
    }

    [Theory]
    [MemberData(nameof(SignInCompleteState))]
    public async Task Get_ValidRequest_RendersExpectedContent(
        TeacherIdentityApplicationDescriptor client,
        string? additionalScopes,
        bool isFirstTimeSignIn,
        bool gotTrn,
        string expectedContentBlock,
        string[] expectedContent)
    {
        // Arrange
        var user = await TestData.CreateUser(hasTrn: gotTrn);

        var authStateHelper = await CreateAuthenticationStateHelper(c => c.Completed(user, firstTimeSignIn: isFirstTimeSignIn), additionalScopes: additionalScopes, client: client);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/complete?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Act
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
        var doc = await response.GetDocument();
        var content = doc.GetElementByTestId(expectedContentBlock)?.InnerHtml;

        foreach (var block in expectedContent)
        {
            Assert.Contains(block, content);
        }
    }

    public static TheoryData<TeacherIdentityApplicationDescriptor, string?, bool, bool, string, string[]> SignInCompleteState => new()
    {
        {
            // Core journey, default client, first time sign in with TRN
            TestClients.DefaultClient,
            CustomScopes.DqtRead,
            true,
            false,
            "first-time-user-content",
            new[]
            {
                "You’ve created a DfE Identity account",
                "Next time, you can sign in just using your email",
                $"Continue to <strong>{TestClients.DefaultClient.DisplayName}"
            }
        },
        {
            // Core journey, default client, first time sign in without TRN
            TestClients.DefaultClient,
            null,
            true,
            false,
            "first-time-user-content",
            new[]
            {
                "You’ve created a DfE Identity account",
                "Next time, you can sign in just using your email",
                $"Continue to <strong>{TestClients.DefaultClient.DisplayName}</strong>"
            }
        },
        {
            // Core journey, default client, known user with TRN
            TestClients.DefaultClient,
            CustomScopes.DqtRead,
            false,
            false,
            "known-user-content",
            new[]
            {
                "You’ve signed in to your DfE Identity account",
                $"Continue to <strong>{TestClients.DefaultClient.DisplayName}</strong>"
            }
        },
        {
            // Core journey, default client, known user without TRN
            TestClients.DefaultClient,
            null,
            false,
            false,
            "known-user-content",
            new[]
            {
                "You’ve signed in to your DfE Identity account",
                $"Continue to <strong>{TestClients.DefaultClient.DisplayName}</strong>"
            }
        },
        {
            // legacy TRN journey, default client, first time sign-in with trn found
            TestClients.LegacyTrnClient,
            CustomScopes.DqtRead,
            true,
            true,
            "legacy-first-time-user-content",
            new[]
            {
                "We’ve finished checking our records",
                "Thank you, we’ve finished checking our records.",
                "If you need to come back to this service later you’ll only need to give us your email address",
            }
        },
        {
            // legacy TRN journey, default client, first time sign-in with trn not found
            TestClients.LegacyTrnClient,
            CustomScopes.DqtRead,
            true,
            false,
            "legacy-first-time-user-content",
            new[]
            {
                "We’ve finished checking our records",
                "You can continue anyway and we’ll try to find your record. Someone may be in touch to ask for more information.",
                "If you need to come back to this service later you’ll only need to give us your email address",
            }
        },
        {
            // legacy TRN journey, default client, known user
            TestClients.LegacyTrnClient,
            CustomScopes.DqtRead,
            false,
            false,
            "legacy-known-user-content",
            new[]
            {
                "Your details",
                "<dt class=\"govuk-summary-list__key\">Name</dt>",
                "<dt class=\"govuk-summary-list__key\">Email address</dt>",
            }
        },
        {
            // legacy TRN journey, Register For NPQ Client, first time sign-in without trn not found
            TestClients.RegisterForNpq,
            CustomScopes.Trn,
            true,
            false,
            "legacy-first-time-user-content",
            new[]
            {
                "Continue to register for an NPQ",
                "Although we could not find your record, you can continue to register for an NPQ.",
                "You’ll need to enter some of your details again.",
            }
        },
        {
            // legacy TRN journey, Register For NPQ Client, first time sign-in without trn not found
            TestClients.RegisterForNpq,
            CustomScopes.DqtRead,
            true,
            false,
            "legacy-first-time-user-content",
            new[]
            {
                "Continue to register for an NPQ",
                "You can continue anyway and we’ll try to find your record. Someone may be in touch to ask for more information.",
                "If you need to come back to this service later you’ll only need to give us your email address",
            }
        }
    };
}

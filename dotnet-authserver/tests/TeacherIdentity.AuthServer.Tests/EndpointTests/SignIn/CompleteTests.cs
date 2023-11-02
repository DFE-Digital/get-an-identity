using TeacherIdentity.AuthServer.Models;
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
        await JourneyIsAlreadyCompleted_DoesNotRedirectToPostSignInUrl(additionalScopes: null, trnRequirementType: null, HttpMethod.Get, "/sign-in/complete");
    }

    [Fact]
    public async Task Get_JourneyHasExpired_RendersErrorPage()
    {
        var user = await TestData.CreateUser(hasTrn: true);
        await JourneyHasExpired_RendersErrorPage(c => c.Completed(user), additionalScopes: null, trnRequirementType: null, HttpMethod.Get, "/sign-in/complete");
    }

    [Theory]
    [MemberData(nameof(SignInCompleteState))]
    public async Task Get_ValidRequest_RendersExpectedContent(
        TeacherIdentityApplicationDescriptor client,
        string? additionalScopes,
        TrnRequirementType? trnRequirementType,
        bool isFirstTimeSignIn,
        TrnLookupStatus? trnLookupStatus,
        bool expectCallbackForm,
        bool trnLookupSupportTicketCreated,
        string[] expectedContent)
    {
        // Arrange
        var user = await TestData.CreateUser(hasTrn: trnLookupStatus == TrnLookupStatus.Found, trnLookupStatus: trnLookupStatus, trnLookupSupportTicketCreated: trnLookupSupportTicketCreated);

        var authStateHelper = await CreateAuthenticationStateHelper(c => c.Completed(user, firstTimeSignIn: isFirstTimeSignIn), additionalScopes, trnRequirementType, trnMatchPolicy: null, client);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/complete?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Act
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
        var doc = await response.GetDocument();
        var content = doc.GetElementsByClassName("govuk-panel").SingleOrDefault()?.InnerHtml;

        foreach (var block in expectedContent)
        {
            Assert.Contains(block, content);
        }

        if (expectCallbackForm)
        {
            Assert.NotEmpty(doc.GetElementsByTagName("form"));
        }
        else
        {
            Assert.Empty(doc.GetElementsByTagName("form"));
        }
    }

    public static TheoryData<TeacherIdentityApplicationDescriptor, string?, TrnRequirementType?, bool, TrnLookupStatus?, bool, bool, string[]> SignInCompleteState => new()
    {
        {
            // Core journey, trn optional client, first time sign in, no TRN lookup
            TestClients.DefaultClient,
            null,
            (TrnRequirementType?)null,
            true,
            (TrnLookupStatus?)null,
            true,
            false,
            new[]
            {
                "You’ve created a DfE Identity account",
                "Next time, you can sign in just using your email",
                $"Continue to <strong>{TestClients.DefaultClient.DisplayName}"
            }
        },
        {
            // Core journey, trn optional client, first time sign in, TRN found
            TestClients.DefaultClient,
            CustomScopes.DqtRead,
            TrnRequirementType.Optional,
            true,
            TrnLookupStatus.Found,
            true,
            false,
            new[]
            {
                "You’ve created a DfE Identity account",
                "We’ve found your details in our records and linked them to your DfE Identity account.",
                "Next time, you can sign in just using your email",
                $"Continue to <strong>{TestClients.DefaultClient.DisplayName}"
            }
        },
        {
            // Core journey, trn optional client, first time sign in, TRN not found
            TestClients.DefaultClient,
            CustomScopes.DqtRead,
            TrnRequirementType.Optional,
            true,
            TrnLookupStatus.Failed,
            true,
            false,
            new[]
            {
                "You’ve created a DfE Identity account",
                "We could not find your details in our records. Our support staff may get in touch to ask for more information. This is so we can link your existing details to your DfE Identity account.",
                "Next time, you can sign in just using your email",
                $"Continue to <strong>{TestClients.DefaultClient.DisplayName}"
            }
        },
        {
            // Core journey, trn optional client, first time sign in, TRN pending
            TestClients.DefaultClient,
            CustomScopes.DqtRead,
            TrnRequirementType.Optional,
            true,
            TrnLookupStatus.Pending,
            true,
            false,
            new[]
            {
                "You’ve created a DfE Identity account",
                "We could not find your details in our records. Our support staff may get in touch to ask for more information. This is so we can link your existing details to your DfE Identity account.",
                "Next time, you can sign in just using your email",
                $"Continue to <strong>{TestClients.DefaultClient.DisplayName}"
            }
        },
        {
            // Core journey, known user sign in, no TRN lookup
            TestClients.DefaultClient,
            null,
            (TrnRequirementType?)null,
            false,
            (TrnLookupStatus?)null,
            true,
            false,
            new[]
            {
                "You’ve signed in to your DfE Identity account",
                $"Continue to <strong>{TestClients.DefaultClient.DisplayName}"
            }
        },
        {
            // Core journey, trn optional client, known user sign in, TRN found
            TestClients.DefaultClient,
            CustomScopes.DqtRead,
            TrnRequirementType.Optional,
            false,
            TrnLookupStatus.Found,
            true,
            false,
            new[]
            {
                "You’ve signed in to your DfE Identity account",
                $"Continue to <strong>{TestClients.DefaultClient.DisplayName}"
            }
        },
        {
            // Core journey, trn optional client, known user sign in, TRN not found
            TestClients.DefaultClient,
            CustomScopes.DqtRead,
            TrnRequirementType.Optional,
            false,
            TrnLookupStatus.Failed,
            true,
            false,
            new[]
            {
                "You’ve signed in to your DfE Identity account",
                $"Continue to <strong>{TestClients.DefaultClient.DisplayName}"
            }
        },
        {
            // Core journey, trn optional client, known user sign in, TRN pending
            TestClients.DefaultClient,
            CustomScopes.DqtRead,
            TrnRequirementType.Optional,
            false,
            TrnLookupStatus.Pending,
            true,
            false,
            new[]
            {
                "You’ve signed in to your DfE Identity account",
                $"Continue to <strong>{TestClients.DefaultClient.DisplayName}"
            }
        },
        {
            // Core journey, trn required client, first time sign in, TRN found
            TestClients.DefaultClient,
            CustomScopes.DqtRead,
            TrnRequirementType.Required,
            true,
            TrnLookupStatus.Found,
            true,
            false,
            new[]
            {
                "You’ve created a DfE Identity account",
                "We’ve found your details in our records and linked them to your DfE Identity account.",
                "Next time, you can sign in just using your email",
                $"Continue to <strong>{TestClients.DefaultClient.DisplayName}"
            }
        },
        {
            // Core journey, trn required client, first time sign in, TRN not found
            TestClients.DefaultClient,
            CustomScopes.DqtRead,
            TrnRequirementType.Required,
            true,
            TrnLookupStatus.Failed,
            false,
            false,
            new[]
            {
                $"You cannot {TestClients.DefaultClient.DisplayName} online",
                "This could be because you do not have teaching qualifications, for example, qualified teacher status (QTS).",
                "You’ve created a DfE Identity account. When you’re eligible to use this service, you can sign in just using your email"
            }
        },
        {
            // Core journey, trn required client, first time sign in, TRN pending
            TestClients.DefaultClient,
            CustomScopes.DqtRead,
            TrnRequirementType.Required,
            true,
            TrnLookupStatus.Pending,
            false,
            true,
            new[]
            {
                $"You cannot {TestClients.DefaultClient.DisplayName} online",
                "We need to do some more checks to see if your details are in our records.",
                "We’ll email you when we’ve completed those checks - we may need some more information.",
                "You’ve created a DfE Identity account. To sign in to this service in the future, you’ll just need your email address"
            }
        },
        {
            // Core journey, trn required client, known user sign in, TRN found
            TestClients.DefaultClient,
            CustomScopes.DqtRead,
            TrnRequirementType.Required,
            false,
            TrnLookupStatus.Found,
            true,
            false,
            new[]
            {
                "You’ve signed in to your DfE Identity account",
                $"Continue to <strong>{TestClients.DefaultClient.DisplayName}"
            }
        },
        {
            // Core journey, trn required client, known user sign in, TRN not found
            TestClients.DefaultClient,
            CustomScopes.DqtRead,
            TrnRequirementType.Required,
            false,
            TrnLookupStatus.Failed,
            false,
            false,
            new[]
            {
                $"You cannot {TestClients.DefaultClient.DisplayName} online",
                "This could be because you do not have teaching qualifications, for example, qualified teacher status (QTS).",
                "You’ve signed in to your DfE Identity account"
            }
        },
        {
            // Core journey, trn required client, known user sign in, TRN pending
            TestClients.DefaultClient,
            CustomScopes.DqtRead,
            TrnRequirementType.Required,
            false,
            TrnLookupStatus.Pending,
            false,
            false,
            new[]
            {
                "We need to find your details in our records so you can use this service.",
                "To fix this problem, please email our support team",
            }
        },
        {
            // Core journey, trn required client, TRN pending, support ticket created
            TestClients.DefaultClient,
            CustomScopes.DqtRead,
            TrnRequirementType.Required,
            false,
            TrnLookupStatus.Pending,
            false,
            true,
            new[]
            {
                $"You cannot {TestClients.DefaultClient.DisplayName} online",
                "We need to do some more checks to see if your details are in our records.",
                "We’ll email you when we’ve completed those checks - we may need some more information.",
                "You’ve signed in to your DfE Identity account"
            }
        }
    };
}

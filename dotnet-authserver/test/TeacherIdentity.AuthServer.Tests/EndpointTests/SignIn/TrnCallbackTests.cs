﻿using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Flurl;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn;

public class TrnCallbackTests : TestBase
{
    public TrnCallbackTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Post_NoAuthenticationStateProvided_ReturnsBadRequest()
    {
        var content = new FormUrlEncodedContentBuilder()
            .Add("user", CreateJwt())
            .ToContent();

        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Post, $"/sign-in/trn-callback", content);
    }

    [Fact]
    public async Task Post_MissingUserQueryParameter_ReturnsError()
    {
        // Arrange
        var authStateHelper = CreateAuthenticationStateHelper(authState => authState.EmailAddress = Faker.Internet.Email());

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn-callback?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_InvalidCallback_ReturnsError()
    {
        // Arrange
        var authStateHelper = CreateAuthenticationStateHelper();

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn-callback?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
                .Add("user", CreateJwt() + "xxx")
                .ToContent()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Post_ValidCallback_CreatesUserCallsDqtApiAndRedirectsToConfirmation(bool hasTrn)
    {
        // Arrange
        var authStateHelper = CreateAuthenticationStateHelper();

        var email = Faker.Internet.Email();
        var firstName = Faker.Name.First();
        var lastName = Faker.Name.Last();
        var dateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth());
        var trn = hasTrn ? TestData.GenerateTrn() : null;

        A.CallTo(() => HostFixture.DqtApiClient!.SetTeacherIdentityInfo(A<DqtTeacherIdentityInfo>.That.Matches(i => i.Trn == trn)))
            .Returns(Task.CompletedTask);

        var jwt = CreateJwt(email, firstName, lastName, dateOfBirth, trn);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn-callback?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
                .Add("user", jwt)
                .ToContent()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal("/sign-in/confirmation", new Url(response.Headers.Location).Path);

        await TestData.WithDbContext(async dbContext =>
        {
            var user = await dbContext.Users.Where(u => u.FirstName == firstName && u.LastName == lastName && u.DateOfBirth == dateOfBirth).SingleOrDefaultAsync();

            Assert.NotNull(user);

            Assert.Equal(authStateHelper.AuthenticationState.UserId, user!.UserId);

            // It's important that the email used to register the user is the one in our auth state,
            // not the one in the callback from Find
            Assert.Equal(authStateHelper.AuthenticationState.EmailAddress, user!.EmailAddress);

            Assert.Equal(firstName, user!.FirstName);
            Assert.Equal(lastName, user!.LastName);
            Assert.Equal(dateOfBirth, user!.DateOfBirth);

            if (hasTrn)
            {
                A.CallTo(() => HostFixture.DqtApiClient
                    !.SetTeacherIdentityInfo(A<DqtTeacherIdentityInfo>.That.Matches(x => x.TsPersonId == user!.UserId.ToString() && x.Trn == trn)))
                    .MustHaveHappenedOnceExactly();
            }
            else
            {
                A.CallTo(() => HostFixture.DqtApiClient
                    !.SetTeacherIdentityInfo(A<DqtTeacherIdentityInfo>.That.Matches(x => x.TsPersonId == user!.UserId.ToString())))
                    .MustNotHaveHappened();
            }
        });
    }

    [Fact]
    public async Task Post_ValidCallbackButApiCallFails_ReturnsError()
    {
        // Arrange
        var authStateHelper = CreateAuthenticationStateHelper();

        var email = Faker.Internet.Email();
        var firstName = Faker.Name.First();
        var lastName = Faker.Name.Last();
        var dateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth());
        var trn = TestData.GenerateTrn();
        authStateHelper.AuthenticationState.Trn = trn;

        A.CallTo(() => HostFixture.DqtApiClient!.SetTeacherIdentityInfo(A<DqtTeacherIdentityInfo>._)).Throws(new InvalidOperationException());

        var jwt = CreateJwt(email, firstName, lastName, dateOfBirth, trn);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn-callback?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
                .Add("user", jwt)
                .ToContent()
        };

        // Act
        var ex = await Record.ExceptionAsync(() => HttpClient.SendAsync(request));

        // Assert
        Assert.NotNull(ex);
    }

    private AuthenticationStateHelper CreateAuthenticationStateHelper() =>
        CreateAuthenticationStateHelper(authState =>
        {
            authState.EmailAddress = Faker.Internet.Email();
            authState.EmailAddressConfirmed = true;
        });

    private const string GenerateRandomTrnSentinel = "0000000";

    private string CreateJwt(
        string? email = null,
        string? firstName = null,
        string? lastName = null,
        DateOnly? dateOfBirth = null,
        string? trn = GenerateRandomTrnSentinel)
    {
        email ??= Faker.Internet.Email();
        firstName ??= Faker.Name.First();
        lastName ??= Faker.Name.Last();
        dateOfBirth ??= DateOnly.FromDateTime(Faker.Identification.DateOfBirth());

        if (trn == GenerateRandomTrnSentinel)
        {
            trn = TestData.GenerateTrn();
        }

        var findALostTrnIntegrationHelper = HostFixture.Services.GetRequiredService<FindALostTrnIntegrationHelper>();

        var pskBytes = Encoding.UTF8.GetBytes(findALostTrnIntegrationHelper.Options.SharedKey);
        var signingCredentials = new SigningCredentials(new SymmetricSecurityKey(pskBytes), SecurityAlgorithms.HmacSha256Signature);

        var subject = new ClaimsIdentity(new[]
        {
            new Claim("email", email!),
            new Claim("birthdate", dateOfBirth!.Value.ToString("yyyy-MM-dd")),
            new Claim("given_name", firstName!),
            new Claim("family_name", lastName!)
        });

        if (trn is not null)
        {
            subject.AddClaim(new Claim("trn", trn!));
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var jwt = tokenHandler.CreateEncodedJwt(new SecurityTokenDescriptor()
        {
            Subject = subject,
            SigningCredentials = signingCredentials
        });

        return jwt;
    }
}

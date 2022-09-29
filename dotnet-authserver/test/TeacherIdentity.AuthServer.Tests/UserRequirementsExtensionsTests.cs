using System.Security.Claims;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TeacherIdentity.AuthServer.Tests;

public class UserRequirementsExtensionsTests
{
    [Theory]
    [MemberData(nameof(GetPermittedUserTypesData))]
    public void GetPermittedUserTypes(UserRequirements userRequirements, UserType[] expectedUserTypes)
    {
        // Arrange

        // Act
        var userTypes = UserRequirementsExtensions.GetPermittedUserTypes(userRequirements);

        // Assert
        Assert.Equal(expectedUserTypes, userTypes);
    }

    [Theory]
    [MemberData(nameof(VerifyStaffUserRequirementsData))]
    public void VerifyStaffUserRequirements(
        UserRequirements userRequirements,
        ClaimsPrincipal principal,
        bool expectedResult)
    {
        // Arrange

        // Act
        var result = userRequirements.VerifyStaffUserRequirements(principal);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    public static TheoryData<UserRequirements, UserType[]> GetPermittedUserTypesData => new TheoryData<UserRequirements, UserType[]>
    {
        { UserRequirements.None, new[] { UserType.Default, UserType.Staff } },
        { UserRequirements.DefaultUserType, new[] { UserType.Default } },
        { UserRequirements.DefaultUserType | UserRequirements.TrnHolder, new[] { UserType.Default } },
        { UserRequirements.StaffUserType, new[] { UserType.Staff } },
        { UserRequirements.StaffUserType | UserRequirements.GetAnIdentityAdmin, new[] { UserType.Staff } },
        { UserRequirements.StaffUserType | UserRequirements.GetAnIdentitySupport, new[] { UserType.Staff } },
    };

    public static TheoryData<UserRequirements, ClaimsPrincipal, bool> VerifyStaffUserRequirementsData
    {
        get
        {
            return new TheoryData<UserRequirements, ClaimsPrincipal, bool>()
            {
                {
                    // Non-Staff user
                    UserRequirements.StaffUserType,
                    CreatePrincipal(UserType.Default),
                    false
                },
                {
                    // Missing a required role
                    UserRequirements.StaffUserType | UserRequirements.GetAnIdentityAdmin,
                    CreatePrincipal(UserType.Staff, StaffRoles.GetAnIdentitySupport),
                    false
                },
                {
                    // No roles required
                    UserRequirements.StaffUserType,
                    CreatePrincipal(UserType.Staff),
                    true
                },
                {
                    // Got all required roles
                    UserRequirements.StaffUserType | UserRequirements.GetAnIdentityAdmin | UserRequirements.GetAnIdentitySupport,
                    CreatePrincipal(UserType.Staff, StaffRoles.GetAnIdentityAdmin, StaffRoles.GetAnIdentitySupport),
                    true
                },
            };

            static ClaimsPrincipal CreatePrincipal(UserType userType, params string[] staffRoles)
            {
                var claims = new List<Claim>()
                {
                    new Claim(Claims.Subject, Guid.NewGuid().ToString()),
                    new Claim(CustomClaims.UserType, userType.ToString())
                };

                foreach (var role in staffRoles)
                {
                    claims.Add(new Claim(Claims.Role, role));
                }

                return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: null, nameType: null, roleType: Claims.Role));
            }
        }
    }
}

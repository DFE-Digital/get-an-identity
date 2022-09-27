using TeacherIdentity.AuthServer.Models;

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

    public static TheoryData<UserRequirements, UserType[]> GetPermittedUserTypesData => new TheoryData<UserRequirements, UserType[]>
    {
        { UserRequirements.None, new[] { UserType.Default, UserType.Staff } },
        { UserRequirements.DefaultUserType, new[] { UserType.Default } },
        { UserRequirements.DefaultUserType | UserRequirements.TrnHolder, new[] { UserType.Default } },
        { UserRequirements.StaffUserType, new[] { UserType.Staff } },
        { UserRequirements.StaffUserType | UserRequirements.GetAnIdentityAdmin, new[] { UserType.Staff } },
        { UserRequirements.StaffUserType | UserRequirements.GetAnIdentitySupport, new[] { UserType.Staff } },
    };
}

using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Tests;

public class UserRequirementsExtensionsTests
{
    [Theory]
    [MemberData(nameof(GetUserTypeData))]
    public void GetUserType(UserRequirements userRequirements, UserType expectedUserType)
    {
        // Arrange

        // Act
        var userType = UserRequirementsExtensions.GetUserType(userRequirements);

        // Assert
        Assert.Equal(expectedUserType, userType);
    }

    public static TheoryData<UserRequirements, UserType> GetUserTypeData => new TheoryData<UserRequirements, UserType>
    {
        { UserRequirements.DefaultUserType, UserType.Default },
        { UserRequirements.DefaultUserType | UserRequirements.TrnHolder, UserType.Default },
        { UserRequirements.StaffUserType, UserType.Staff },
        { UserRequirements.StaffUserType | UserRequirements.GetAnIdentityAdmin, UserType.Staff },
        { UserRequirements.StaffUserType | UserRequirements.GetAnIdentitySupport, UserType.Staff },
    };
}

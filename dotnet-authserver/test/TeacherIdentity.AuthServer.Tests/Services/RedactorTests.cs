using TeacherIdentity.AuthServer.Services;

namespace TeacherIdentity.AuthServer.Tests.Services;

public class RedactorTests
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("joe", "j****")]
    [InlineData("joe@gmail.com", "j****@****.com")]
    [InlineData("joe.bloggs@gmail.com", "j****@****.com")]
    [InlineData("joe.bloggs@hotmail.co.uk", "j****@****.co.uk")]
    [InlineData("joe.bloggs@education.gov.uk", "j****@****.gov.uk")]
    [InlineData("joe.bloggs@digital.education.gov.uk", "j****@****.gov.uk")]
    [InlineData("joe.bloggs@example.digital", "j****@****.digital")]
    public void RedactEmail(string email, string expectedResult)
    {
        var redactor = new Redactor();
        var result = redactor.RedactEmail(email);
        Assert.Equal(expectedResult, result);
    }
}

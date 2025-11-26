using System.Net;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.EndToEndTests.Admin.MergeUser;

public class SelectUser : IClassFixture<HostFixture>
{
    private readonly HostFixture _hostFixture;

    public SelectUser(HostFixture hostFixture)
    {
        _hostFixture = hostFixture;
        _hostFixture.OnTestStarting();
    }

    [Fact]
    public async Task StaffUser_WithoutMergeUserRole_ReturnsForbidden()
    {
        var staffUser = await _hostFixture.TestData.CreateUser(userType: UserType.Staff,
            staffRoles: new[] { StaffRoles.GetAnIdentitySupport });
        var teacherUser1 = await _hostFixture.TestData.CreateUser(hasTrn: true, userType: UserType.Teacher,
            staffRoles: Array.Empty<string>());
        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{HostFixture.AuthServerBaseUrl}/admin/users");

        await page.SubmitEmailPage(staffUser.EmailAddress);

        await page.SubmitEmailConfirmationPage();
        var response = await page.GotoAsync($"{HostFixture.AuthServerBaseUrl}/admin/users/{teacherUser1.UserId}/merge");
        Assert.Equal((int)HttpStatusCode.Forbidden, response!.Status);
    }

    [Fact]
    public async Task StaffUser_WithMergeUserRole_CanAccessMergeUserSelectUserPage()
    {
        var staffUser = await _hostFixture.TestData.CreateUser(userType: UserType.Staff, staffRoles: StaffRoles.All);
        var teacherUser1 = await _hostFixture.TestData.CreateUser(hasTrn: true, userType: UserType.Teacher,
            staffRoles: Array.Empty<string>());
        var teacherUser2 =
            await _hostFixture.TestData.CreateUser(userType: UserType.Teacher, staffRoles: Array.Empty<string>());

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{HostFixture.AuthServerBaseUrl}/admin/staff");
        await page.SubmitEmailPage(staffUser.EmailAddress);
        await page.SubmitEmailConfirmationPage();
        await page.WaitForUrlPathAsync("/admin/staff");
        await page.GotoAsync($"{HostFixture.AuthServerBaseUrl}/admin/users");
        await page.WaitForUrlPathAsync("/admin/users");
        await page.GotoAsync($"{HostFixture.AuthServerBaseUrl}/admin/users/{teacherUser1.UserId}/merge");
        await page.WaitForUrlPathAsync($"/admin/users/{teacherUser1.UserId}/merge");
    }
}

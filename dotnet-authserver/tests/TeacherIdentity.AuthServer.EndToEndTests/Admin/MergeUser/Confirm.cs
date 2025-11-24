using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.EndToEndTests.Admin.MergeUser;

public class Confirm : IClassFixture<HostFixture>
{
    private readonly HostFixture _hostFixture;

    public Confirm(HostFixture hostFixture)
    {
        _hostFixture = hostFixture;
        _hostFixture.OnTestStarting();
    }

    [Fact]
    public async Task StaffUser_WithMergeUserRole_CanMergeTwoUsers()
    {
        var staffUser = await _hostFixture.TestData.CreateUser(userType: UserType.Staff, staffRoles:new [] { StaffRoles.GetAnIdentitySupportMergeUser, StaffRoles.GetAnIdentitySupport });
        var teacherUser1 = await _hostFixture.TestData.CreateUser(hasTrn: false, trnVerificationLevel: TrnVerificationLevel.Low);
        var teacherUser2 = await _hostFixture.TestData.CreateUser(hasTrn: false);

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{HostFixture.AuthServerBaseUrl}/admin/users/{teacherUser1.UserId}/merge");

        await page.SubmitEmailPage(staffUser.EmailAddress);

        await page.SubmitEmailConfirmationPage();
        await page.WaitForUrlPathAsync($"/admin/users/{teacherUser1.UserId}/merge");
        await page.SubmitMergeUserPage(teacherUser2.UserId.ToString());
        await page.WaitForUrlPathAsync($"/admin/users/{teacherUser1.UserId}/merge/{teacherUser2.UserId}/confirm");
        await page.ClickContinueButton();
        await page.WaitForUrlPathAsync($"/admin/users/{teacherUser1.UserId}");
    }
}

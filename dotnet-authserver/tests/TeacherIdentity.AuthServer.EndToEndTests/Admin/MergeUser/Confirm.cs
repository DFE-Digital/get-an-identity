using Moq;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.DqtApi;

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
        var staffUser = await _hostFixture.TestData.CreateUser(userType: UserType.Staff, staffRoles: new[] { StaffRoles.GetAnIdentitySupportMergeUser, StaffRoles.GetAnIdentitySupport });
        var teacherUser1 = await _hostFixture.TestData.CreateUser(hasTrn: true, trnVerificationLevel: TrnVerificationLevel.Low);
        var teacherUser2 = await _hostFixture.TestData.CreateUser(hasTrn: false);
        _hostFixture.DqtApiClient.Setup(mock => mock.GetTeacherByTrn(teacherUser1.Trn!, It.IsAny<CancellationToken>())).ReturnsAsync(new TeacherInfo()
        {
            DateOfBirth = teacherUser1.DateOfBirth,
            Email = teacherUser1.EmailAddress,
            FirstName = teacherUser1.FirstName,
            MiddleName = teacherUser1.MiddleName ?? string.Empty,
            LastName = teacherUser1.LastName,
            NationalInsuranceNumber = teacherUser1.NationalInsuranceNumber,
            PendingDateOfBirthChange = false,
            PendingNameChange = false,
            Trn = teacherUser1.Trn!,
            Alerts = Array.Empty<AlertInfo>(),
            AllowIdSignInWithProhibitions = false
        });

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

    [Fact]
    public async Task StaffUser_WithMergeTwoUsersWithTrn_CanMergeTwoUsers()
    {
        var staffUser = await _hostFixture.TestData.CreateUser(userType: UserType.Staff, staffRoles: new[] { StaffRoles.GetAnIdentitySupportMergeUser, StaffRoles.GetAnIdentitySupport });
        var teacherUser1 = await _hostFixture.TestData.CreateUser(hasTrn: true, trnVerificationLevel: TrnVerificationLevel.Low);
        var teacherUser2 = await _hostFixture.TestData.CreateUser(hasTrn: true);
        _hostFixture.DqtApiClient.Setup(mock => mock.GetTeacherByTrn(teacherUser1.Trn!, It.IsAny<CancellationToken>())).ReturnsAsync(new TeacherInfo()
        {
            DateOfBirth = null,
            //DateOfBirth = teacherUser1.DateOfBirth,
            Email = teacherUser1.EmailAddress,
            FirstName = teacherUser1.FirstName,
            MiddleName = teacherUser1.MiddleName ?? string.Empty,
            LastName = teacherUser1.LastName,
            NationalInsuranceNumber = teacherUser1.NationalInsuranceNumber,
            PendingDateOfBirthChange = false,
            PendingNameChange = false,
            Trn = teacherUser1.Trn!,
            Alerts = Array.Empty<AlertInfo>(),
            AllowIdSignInWithProhibitions = false
        });
        _hostFixture.DqtApiClient.Setup(mock => mock.GetTeacherByTrn(teacherUser2.Trn!, It.IsAny<CancellationToken>())).ReturnsAsync(new TeacherInfo()
        {
            DateOfBirth = null,
            //DateOfBirth = teacherUser2.DateOfBirth,
            Email = teacherUser2.EmailAddress,
            FirstName = teacherUser2.FirstName,
            MiddleName = teacherUser2.MiddleName ?? string.Empty,
            LastName = teacherUser2.LastName,
            NationalInsuranceNumber = teacherUser2.NationalInsuranceNumber,
            PendingDateOfBirthChange = false,
            PendingNameChange = false,
            Trn = teacherUser2.Trn!,
            Alerts = Array.Empty<AlertInfo>(),
            AllowIdSignInWithProhibitions = false
        });

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{HostFixture.AuthServerBaseUrl}/admin/users/{teacherUser2.UserId}/merge");

        await page.SubmitEmailPage(staffUser.EmailAddress);

        await page.SubmitEmailConfirmationPage();
        await page.WaitForUrlPathAsync($"/admin/users/{teacherUser2.UserId}/merge");
        await page.SubmitMergeUserPage(teacherUser1.UserId.ToString());
        await page.WaitForUrlPathAsync($"/admin/users/{teacherUser2.UserId}/merge/{teacherUser1.UserId}/confirm");
        await page.ClickContinueButton();
        await page.WaitForUrlPathAsync($"/admin/users/{teacherUser2.UserId}");
    }
}

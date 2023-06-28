using TeacherIdentity.AuthServer.Notifications;
using TeacherIdentity.AuthServer.Services.DqtApi;
using TeacherIdentity.AuthServer.Services.DqtEvidence;
using TeacherIdentity.AuthServer.Services.Notification;
using TeacherIdentity.AuthServer.Services.UserImport;
using TeacherIdentity.AuthServer.Services.UserVerification;
using TeacherIdentity.AuthServer.Services.Zendesk;

namespace TeacherIdentity.AuthServer.Tests;

public class TestScopedServices
{
    private static readonly AsyncLocal<TestScopedServices> _current = new();

    public TestScopedServices()
    {
        Clock = new();
        DqtApiClient = new();
        NotificationSender = new();
        NotificationPublisher = new();
        RateLimitStore = new();
        SpyRegistry = new();
        UserImportCsvStorageService = new();
        DqtEvidenceStorageService = new();
        ZendeskApiWrapper = new();

        DqtApiClient.Setup(mock => mock.FindTeachers(It.IsAny<FindTeachersRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FindTeachersResponse() { Results = Array.Empty<FindTeachersResponseResult>() });
    }

    public static TestScopedServices Current => _current.Value ??= new();

    public TestClock Clock { get; }

    public Mock<IDqtApiClient> DqtApiClient { get; }

    public Mock<INotificationSender> NotificationSender { get; }

    public Mock<INotificationPublisher> NotificationPublisher { get; }

    public Mock<IRateLimitStore> RateLimitStore { get; }

    public SpyRegistry SpyRegistry { get; }

    public Mock<IUserImportStorageService> UserImportCsvStorageService { get; }

    public Mock<IDqtEvidenceStorageService> DqtEvidenceStorageService { get; }

    public Mock<IZendeskApiWrapper> ZendeskApiWrapper { get; }
}

using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Notifications.Messages;
using TeacherIdentity.AuthServer.Notifications.WebHooks;

namespace TeacherIdentity.AuthServer.Tests.Notifications;

public class WebHookNotificationPublisherTests
{
    [Fact]
    public async Task PublishNotification_InvokesWebHookSenderForEachWebHook()
    {
        // Arrange
        var senderMock = new Mock<IWebHookNotificationSender>();
        var publisher = new WebHookNotificationPublisher(senderMock.Object);

        var notification = new NotificationEnvelope()
        {
            Message = new EmptyMessage(),
            MessageType = "Empty",
            TimeUtc = DateTime.UtcNow
        };

        // Act
        await publisher.PublishNotification(notification);

        // Assert
        senderMock.Verify(mock => mock.SendNotification(notification, It.IsAny<WebHook>()));
    }
}

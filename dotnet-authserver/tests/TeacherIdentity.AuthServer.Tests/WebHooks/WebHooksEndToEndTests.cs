using System.Text.Json;
using System.Text.Json.Nodes;
using Optional;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Notifications;
using TeacherIdentity.AuthServer.Notifications.Messages;
using TeacherIdentity.AuthServer.Notifications.WebHooks;
using User = TeacherIdentity.AuthServer.Notifications.Messages.User;

namespace TeacherIdentity.AuthServer.Tests.WebHooks;

[Collection(nameof(DisableParallelization))]
public class WebHooksEndToEndTests : TestBase
{
    public WebHooksEndToEndTests(WebHooksHostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Theory]
    [InlineData(WebHookMessageTypes.UserUpdated)]
    [InlineData(WebHookMessageTypes.UserUpdated | WebHookMessageTypes.UserMerged)]
    [InlineData(WebHookMessageTypes.UserUpdated | WebHookMessageTypes.UserCreated)]
    [InlineData(WebHookMessageTypes.UserMerged)]
    [InlineData(WebHookMessageTypes.UserMerged | WebHookMessageTypes.UserCreated)]
    [InlineData(WebHookMessageTypes.UserCreated)]
    [InlineData(WebHookMessageTypes.All)]
    public async Task Ping_WhenPublished_IsReceivedByWebHookEndpointRegardlessOfMessageTypesSubscribedTo(WebHookMessageTypes webHookMessageTypesSubscribedTo)
    {
        // Arrange
        var notificationPublisher = HostFixture.Services.GetRequiredService<INotificationPublisher>();
        var secret = "Thisismywebhooksecret";
        await ConfigureTestWebHook(webHookMessageTypesSubscribedTo, secret);

        // Act
        var pingNotification = new NotificationEnvelope()
        {
            NotificationId = Guid.NewGuid(),
            Message = new PingMessage()
            {
                WebHookId = HostFixture.TestWebHookId
            },
            MessageType = PingMessage.MessageTypeName,
            TimeUtc = HostFixture.Clock.UtcNow
        };

        await notificationPublisher.PublishNotification(pingNotification);

        // Assert
        WebHookRequestObserver.AssertWebHookRequestsReceived(
            r =>
            {
                Assert.NotNull(r.ContentType);
                Assert.NotNull(r.Signature);
                Assert.NotNull(r.Body);
                Assert.Equal("application/json", r.ContentType);
                var expectedSignature = WebHookNotificationSender.CalculateSignature(secret, r.Body);
                Assert.Equal(expectedSignature, r.Signature);

                var expectedJson = JsonSerializer.SerializeToNode(new
                {
                    notificationId = pingNotification.NotificationId,
                    message = new
                    {
                    },
                    messageType = PingMessage.MessageTypeName,
                    timeUtc = HostFixture.Clock.UtcNow
                });

                AssertEx.JsonEquals(
                    expectedJson,
                    JsonNode.Parse(r.Body));
            });
    }

    [Theory]
    [InlineData(WebHookMessageTypes.UserUpdated, UserUpdatedEventChanges.DateOfBirth | UserUpdatedEventChanges.EmailAddress | UserUpdatedEventChanges.FirstName | UserUpdatedEventChanges.MiddleName | UserUpdatedEventChanges.LastName | UserUpdatedEventChanges.PreferredName | UserUpdatedEventChanges.Trn | UserUpdatedEventChanges.MobileNumber | UserUpdatedEventChanges.TrnLookupStatus, true)]
    [InlineData(WebHookMessageTypes.UserUpdated, UserUpdatedEventChanges.DateOfBirth, true)]
    [InlineData(WebHookMessageTypes.UserUpdated, UserUpdatedEventChanges.EmailAddress, true)]
    [InlineData(WebHookMessageTypes.UserUpdated, UserUpdatedEventChanges.FirstName, true)]
    [InlineData(WebHookMessageTypes.UserUpdated, UserUpdatedEventChanges.MiddleName, true)]
    [InlineData(WebHookMessageTypes.UserUpdated, UserUpdatedEventChanges.LastName, true)]
    [InlineData(WebHookMessageTypes.UserUpdated, UserUpdatedEventChanges.PreferredName, true)]
    [InlineData(WebHookMessageTypes.UserUpdated, UserUpdatedEventChanges.Trn, true)]
    [InlineData(WebHookMessageTypes.UserUpdated, UserUpdatedEventChanges.MobileNumber, true)]
    [InlineData(WebHookMessageTypes.UserUpdated, UserUpdatedEventChanges.TrnLookupStatus, true)]
    [InlineData(WebHookMessageTypes.UserUpdated | WebHookMessageTypes.UserMerged, UserUpdatedEventChanges.DateOfBirth, true)]
    [InlineData(WebHookMessageTypes.UserUpdated | WebHookMessageTypes.UserCreated, UserUpdatedEventChanges.DateOfBirth, true)]
    [InlineData(WebHookMessageTypes.UserMerged, UserUpdatedEventChanges.DateOfBirth, false)]
    [InlineData(WebHookMessageTypes.UserMerged | WebHookMessageTypes.UserCreated, UserUpdatedEventChanges.DateOfBirth, false)]
    [InlineData(WebHookMessageTypes.UserCreated, UserUpdatedEventChanges.DateOfBirth, false)]
    [InlineData(WebHookMessageTypes.All, UserUpdatedEventChanges.DateOfBirth, true)]
    public async Task UserUpdated_WhenPublished_IsOnlySentToWebHookEndpointsWhichSubscribe(
        WebHookMessageTypes webHookMessageTypesSubscribedTo,
        UserUpdatedEventChanges userUpdatedEventChanges,
        bool expectedToReceiveMessage)
    {
        // Arrange
        var notificationPublisher = HostFixture.Services.GetRequiredService<INotificationPublisher>();
        var secret = "Thisismywebhooksecret";
        await ConfigureTestWebHook(webHookMessageTypesSubscribedTo, secret);

        var user = new User()
        {
            UserId = new Guid("3828dc03-7500-402a-beeb-acd45b635fc3"),
            DateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth()),
            EmailAddress = Faker.Internet.Email(),
            FirstName = Faker.Name.First(),
            MiddleName = Faker.Name.Middle(),
            LastName = Faker.Name.Last(),
            PreferredName = Faker.Name.FullName(),
            MobileNumber = Faker.Phone.Number(),
            Trn = "7754311",
            TrnLookupStatus = TrnLookupStatus.Found,
        };

        var UserUpdatedMessageChanges = new UserUpdatedMessageChanges
        {
            DateOfBirth = userUpdatedEventChanges.HasFlag(UserUpdatedEventChanges.DateOfBirth) ? Option.Some(user.DateOfBirth) : default,
            EmailAddress = userUpdatedEventChanges.HasFlag(UserUpdatedEventChanges.EmailAddress) ? Option.Some(user.EmailAddress) : default,
            FirstName = userUpdatedEventChanges.HasFlag(UserUpdatedEventChanges.FirstName) ? Option.Some(user.FirstName) : default,
            MiddleName = userUpdatedEventChanges.HasFlag(UserUpdatedEventChanges.MiddleName) ? Option.Some<string?>(user.MiddleName) : default,
            LastName = userUpdatedEventChanges.HasFlag(UserUpdatedEventChanges.LastName) ? Option.Some(user.LastName) : default,
            PreferredName = userUpdatedEventChanges.HasFlag(UserUpdatedEventChanges.PreferredName) ? Option.Some<string?>(user.PreferredName) : default,
            Trn = userUpdatedEventChanges.HasFlag(UserUpdatedEventChanges.Trn) ? Option.Some<string?>(user.Trn) : default,
            MobileNumber = userUpdatedEventChanges.HasFlag(UserUpdatedEventChanges.MobileNumber) ? Option.Some<string?>(user.MobileNumber) : default,
            TrnLookupStatus = userUpdatedEventChanges.HasFlag(UserUpdatedEventChanges.TrnLookupStatus) ? Option.Some(user.TrnLookupStatus) : default
        };

        // Act
        var userUpdatedNotification = new NotificationEnvelope()
        {
            NotificationId = Guid.NewGuid(),
            Message = new UserUpdatedMessage()
            {
                User = user,
                Changes = UserUpdatedMessageChanges

            },
            MessageType = UserUpdatedMessage.MessageTypeName,
            TimeUtc = HostFixture.Clock.UtcNow
        };

        await notificationPublisher.PublishNotification(userUpdatedNotification);

        // Assert
        if (expectedToReceiveMessage)
        {
            WebHookRequestObserver.AssertWebHookRequestsReceived(
            r =>
            {
                Assert.NotNull(r.ContentType);
                Assert.NotNull(r.Signature);
                Assert.NotNull(r.Body);
                Assert.Equal("application/json", r.ContentType);
                var expectedSignature = WebHookNotificationSender.CalculateSignature(secret, r.Body);
                Assert.Equal(expectedSignature, r.Signature);

                var expectedJson = JsonSerializer.SerializeToNode(new
                {
                    notificationId = userUpdatedNotification.NotificationId,
                    message = new
                    {
                        user = new
                        {
                            userId = user.UserId,
                            dateOfBirth = user.DateOfBirth,
                            emailAddress = user.EmailAddress,
                            firstName = user.FirstName,
                            middleName = user.MiddleName,
                            lastName = user.LastName,
                            preferredName = user.PreferredName,
                            mobileNumber = user.MobileNumber,
                            trn = user.Trn,
                            trnLookupStatus = user.TrnLookupStatus.ToString()
                        },
                        changes = new
                        {
                            dateOfBirth = user.DateOfBirth,
                            emailAddress = user.EmailAddress,
                            firstName = user.FirstName,
                            middleName = user.MiddleName,
                            lastName = user.LastName,
                            preferredName = user.PreferredName,
                            mobileNumber = user.MobileNumber,
                            trn = user.Trn,
                            trnLookupStatus = user.TrnLookupStatus.ToString()
                        }
                    },
                    messageType = UserUpdatedMessage.MessageTypeName,
                    timeUtc = HostFixture.Clock.UtcNow
                });

                var changes = expectedJson!["message"]!["changes"]!.AsObject();
                if (!userUpdatedEventChanges.HasFlag(UserUpdatedEventChanges.DateOfBirth))
                {
                    changes.Remove("dateOfBirth");
                }

                if (!userUpdatedEventChanges.HasFlag(UserUpdatedEventChanges.EmailAddress))
                {
                    changes.Remove("emailAddress");
                }

                if (!userUpdatedEventChanges.HasFlag(UserUpdatedEventChanges.FirstName))
                {
                    changes.Remove("firstName");
                }

                if (!userUpdatedEventChanges.HasFlag(UserUpdatedEventChanges.MiddleName))
                {
                    changes.Remove("middleName");
                }

                if (!userUpdatedEventChanges.HasFlag(UserUpdatedEventChanges.LastName))
                {
                    changes.Remove("lastName");
                }

                if (!userUpdatedEventChanges.HasFlag(UserUpdatedEventChanges.PreferredName))
                {
                    changes.Remove("preferredName");
                }

                if (!userUpdatedEventChanges.HasFlag(UserUpdatedEventChanges.MobileNumber))
                {
                    changes.Remove("mobileNumber");
                }

                if (!userUpdatedEventChanges.HasFlag(UserUpdatedEventChanges.Trn))
                {
                    changes.Remove("trn");
                }

                if (!userUpdatedEventChanges.HasFlag(UserUpdatedEventChanges.TrnLookupStatus))
                {
                    changes.Remove("trnLookupStatus");
                }

                AssertEx.JsonEquals(
                    expectedJson,
                    JsonNode.Parse(r.Body));
            });
        }
        else
        {
            WebHookRequestObserver.AssertWebHookRequestsReceived(Array.Empty<Action<WebHookRequest>>());
        }
    }

    [Theory]
    [InlineData(WebHookMessageTypes.UserUpdated, false)]
    [InlineData(WebHookMessageTypes.UserUpdated | WebHookMessageTypes.UserMerged, false)]
    [InlineData(WebHookMessageTypes.UserUpdated | WebHookMessageTypes.UserCreated, true)]
    [InlineData(WebHookMessageTypes.UserMerged, false)]
    [InlineData(WebHookMessageTypes.UserMerged | WebHookMessageTypes.UserCreated, true)]
    [InlineData(WebHookMessageTypes.UserCreated, true)]
    [InlineData(WebHookMessageTypes.All, true)]
    public async Task UserCreated_WhenPublished_IsOnlySentToWebHookEndpointsWhichSubscribe(
        WebHookMessageTypes webHookMessageTypesSubscribedTo,
        bool expectedToReceiveMessage)
    {
        // Arrange
        var notificationPublisher = HostFixture.Services.GetRequiredService<INotificationPublisher>();
        var secret = "Thisismywebhooksecret";
        await ConfigureTestWebHook(webHookMessageTypesSubscribedTo, secret);

        var user = new User()
        {
            UserId = new Guid("3828dc03-7500-402a-beeb-acd45b635fc3"),
            DateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth()),
            EmailAddress = Faker.Internet.Email(),
            FirstName = Faker.Name.First(),
            MiddleName = Faker.Name.Middle(),
            LastName = Faker.Name.Last(),
            PreferredName = Faker.Name.FullName(),
            MobileNumber = Faker.Phone.Number(),
            Trn = "7754311",
            TrnLookupStatus = TrnLookupStatus.Found,
        };

        // Act
        var userCreatedNotification = new NotificationEnvelope()
        {
            NotificationId = Guid.NewGuid(),
            Message = new UserCreatedMessage()
            {
                User = user

            },
            MessageType = UserCreatedMessage.MessageTypeName,
            TimeUtc = HostFixture.Clock.UtcNow
        };

        await notificationPublisher.PublishNotification(userCreatedNotification);

        // Assert
        if (expectedToReceiveMessage)
        {
            WebHookRequestObserver.AssertWebHookRequestsReceived(
            r =>
            {
                Assert.NotNull(r.ContentType);
                Assert.NotNull(r.Signature);
                Assert.NotNull(r.Body);
                Assert.Equal("application/json", r.ContentType);
                var expectedSignature = WebHookNotificationSender.CalculateSignature(secret, r.Body);
                Assert.Equal(expectedSignature, r.Signature);

                var expectedJson = JsonSerializer.SerializeToNode(new
                {
                    notificationId = userCreatedNotification.NotificationId,
                    message = new
                    {
                        user = new
                        {
                            userId = user.UserId,
                            dateOfBirth = user.DateOfBirth,
                            emailAddress = user.EmailAddress,
                            firstName = user.FirstName,
                            middleName = user.MiddleName,
                            lastName = user.LastName,
                            preferredName = user.PreferredName,
                            mobileNumber = user.MobileNumber,
                            trn = user.Trn,
                            trnLookupStatus = user.TrnLookupStatus.ToString()
                        }
                    },
                    messageType = UserCreatedMessage.MessageTypeName,
                    timeUtc = HostFixture.Clock.UtcNow
                });

                AssertEx.JsonEquals(
                    expectedJson,
                    JsonNode.Parse(r.Body));
            });
        }
        else
        {
            WebHookRequestObserver.AssertWebHookRequestsReceived(Array.Empty<Action<WebHookRequest>>());
        }
    }

    [Theory]
    [InlineData(WebHookMessageTypes.UserUpdated, false)]
    [InlineData(WebHookMessageTypes.UserUpdated | WebHookMessageTypes.UserMerged, true)]
    [InlineData(WebHookMessageTypes.UserUpdated | WebHookMessageTypes.UserCreated, false)]
    [InlineData(WebHookMessageTypes.UserMerged, true)]
    [InlineData(WebHookMessageTypes.UserMerged | WebHookMessageTypes.UserCreated, true)]
    [InlineData(WebHookMessageTypes.UserCreated, false)]
    [InlineData(WebHookMessageTypes.All, true)]
    public async Task UserMerged_WhenPublished_IsOnlySentToWebHookEndpointsWhichSubscribe(
        WebHookMessageTypes webHookMessageTypesSubscribedTo,
        bool expectedToReceiveMessage)
    {
        // Arrange
        var notificationPublisher = HostFixture.Services.GetRequiredService<INotificationPublisher>();
        var secret = "Thisismywebhooksecret";
        await ConfigureTestWebHook(webHookMessageTypesSubscribedTo, secret);

        var user = new User()
        {
            UserId = new Guid("3828dc03-7500-402a-beeb-acd45b635fc3"),
            DateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth()),
            EmailAddress = Faker.Internet.Email(),
            FirstName = Faker.Name.First(),
            MiddleName = Faker.Name.Middle(),
            LastName = Faker.Name.Last(),
            PreferredName = Faker.Name.FullName(),
            MobileNumber = Faker.Phone.Number(),
            Trn = "7754311",
            TrnLookupStatus = TrnLookupStatus.Found,
        };

        var mergedUserId = Guid.NewGuid();

        // Act
        var userCreatedNotification = new NotificationEnvelope()
        {
            NotificationId = Guid.NewGuid(),
            Message = new UserMergedMessage()
            {
                MasterUser = user,
                MergedUserId = mergedUserId
            },
            MessageType = UserMergedMessage.MessageTypeName,
            TimeUtc = HostFixture.Clock.UtcNow
        };

        await notificationPublisher.PublishNotification(userCreatedNotification);

        // Assert
        if (expectedToReceiveMessage)
        {
            WebHookRequestObserver.AssertWebHookRequestsReceived(
            r =>
            {
                Assert.NotNull(r.ContentType);
                Assert.NotNull(r.Signature);
                Assert.NotNull(r.Body);
                Assert.Equal("application/json", r.ContentType);
                var expectedSignature = WebHookNotificationSender.CalculateSignature(secret, r.Body);
                Assert.Equal(expectedSignature, r.Signature);

                var expectedJson = JsonSerializer.SerializeToNode(new
                {
                    notificationId = userCreatedNotification.NotificationId,
                    message = new
                    {
                        masterUser = new
                        {
                            userId = user.UserId,
                            dateOfBirth = user.DateOfBirth,
                            emailAddress = user.EmailAddress,
                            firstName = user.FirstName,
                            middleName = user.MiddleName,
                            lastName = user.LastName,
                            preferredName = user.PreferredName,
                            mobileNumber = user.MobileNumber,
                            trn = user.Trn,
                            trnLookupStatus = user.TrnLookupStatus.ToString()
                        },
                        mergedUserId = mergedUserId
                    },
                    messageType = UserMergedMessage.MessageTypeName,
                    timeUtc = HostFixture.Clock.UtcNow
                });

                AssertEx.JsonEquals(
                    expectedJson,
                    JsonNode.Parse(r.Body));
            });
        }
        else
        {
            WebHookRequestObserver.AssertWebHookRequestsReceived(Array.Empty<Action<WebHookRequest>>());
        }
    }
}

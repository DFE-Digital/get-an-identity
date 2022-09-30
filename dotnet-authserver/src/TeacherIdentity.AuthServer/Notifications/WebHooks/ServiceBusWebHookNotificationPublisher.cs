using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Notifications.Messages;
using TeacherIdentity.AuthServer.Notifications.WebHooks.ServiceBusMessages;

namespace TeacherIdentity.AuthServer.Notifications.WebHooks;

public sealed class ServiceBusWebHookNotificationPublisher : WebHookNotificationPublisher, IAsyncDisposable, IHostedService
{
    private static readonly TimeSpan[] _retryIntervals = new[]
    {
        TimeSpan.FromSeconds(30),
        TimeSpan.FromMinutes(2),
        TimeSpan.FromMinutes(10),
        TimeSpan.FromHours(1),
        TimeSpan.FromHours(2),
        TimeSpan.FromHours(4),
        TimeSpan.FromHours(8),
    };

    private readonly ServiceBusSender _serviceBusSender;
    private readonly ServiceBusProcessor _serviceBusProcessor;
    private readonly ILogger<ServiceBusWebHookNotificationPublisher> _logger;

    public ServiceBusWebHookNotificationPublisher(
        ServiceBusClient serviceBusClient,
        IOptions<ServiceBusWebHookOptions> optionsAccessor,
        ILogger<ServiceBusWebHookNotificationPublisher> logger,
        IWebHookNotificationSender sender,
        IDbContextFactory<TeacherIdentityServerDbContext> dbContextFactory,
        IMemoryCache memoryCache,
        IOptions<WebHookOptions> webHookOptionsAccessor)
        : base(sender, dbContextFactory, memoryCache, webHookOptionsAccessor)
    {
        _serviceBusSender = serviceBusClient.CreateSender(optionsAccessor.Value.QueueName);

        _serviceBusProcessor = serviceBusClient.CreateProcessor(
            optionsAccessor.Value.QueueName,
            new ServiceBusProcessorOptions()
            {
                AutoCompleteMessages = false
            });
        _serviceBusProcessor.ProcessMessageAsync += ProcessMessage;
        _serviceBusProcessor.ProcessErrorAsync += ProcessError;

        _logger = logger;
    }

    public async ValueTask DisposeAsync()
    {
        await _serviceBusProcessor.DisposeAsync();
        await _serviceBusSender.DisposeAsync();
    }

    public override async Task PublishNotification(NotificationEnvelope notification)
    {
        var payload = SerializeNotification(notification);

        var webHooks = await GetWebHooksForNotification(notification);

        var messages = webHooks
            .Select(wh => new SendNotificationToWebHook()
            {
                Endpoint = wh.Endpoint,
                Payload = payload
            })
            .Select(msg => new ServiceBusMessage(BinaryData.FromObjectAsJson(msg))
            {
                Subject = nameof(SendNotificationToWebHook),
                ApplicationProperties =
                {
                    { ApplicationPropertiesKeys.Endpoint, msg.Endpoint }
                }
            });

        await _serviceBusSender.SendMessagesAsync(messages);
    }

    private async Task ProcessMessage(ProcessMessageEventArgs arg)
    {
        var message = arg.Message;

        var retryNumber = message.ApplicationProperties.TryGetValue(ApplicationPropertiesKeys.RetryNumber, out var retryNumberObj) ?
            (int)retryNumberObj :
            0;

        try
        {
            if (message.Subject is nameof(SendNotificationToWebHook))
            {
                await ProcessSendNotificationToWebHookMessage(message.Body.ToObjectFromJson<SendNotificationToWebHook>());
            }
            else
            {
                _logger.LogWarning("Received an unknown message subject: '{Subject}'.", message.Subject);
            }

            await arg.CompleteMessageAsync(message);
        }
        catch (Exception ex)
        {
            var maxRetries = _retryIntervals.Length;

            if (retryNumber >= maxRetries)
            {
                _logger.LogError(ex, "Failed processing message after {RetryNumber} retries.", retryNumber);

                await arg.DeadLetterMessageAsync(message, deadLetterReason: "Could not deliver web hook", deadLetterErrorDescription: ex.ToString());
                return;
            }

            // Enqueue a clone of this message with a future processing time, based on the retry number and _retryIntervals
            retryNumber++;
            var enqueueInterval = _retryIntervals[retryNumber - 1];
            var enqueueTime = DateTimeOffset.UtcNow.Add(enqueueInterval);

            var retryMessage = new ServiceBusMessage(message);
            retryMessage.ApplicationProperties[ApplicationPropertiesKeys.RetryNumber] = retryNumber;
            retryMessage.ScheduledEnqueueTime = enqueueTime;

            await _serviceBusSender.SendMessageAsync(retryMessage);

            await arg.CompleteMessageAsync(message);
        }
    }

    private Task ProcessError(ProcessErrorEventArgs arg)
    {
        _logger.LogError(arg.Exception, message: "Service Bus error.");

        return Task.CompletedTask;
    }

    private async Task ProcessSendNotificationToWebHookMessage(SendNotificationToWebHook message)
    {
        await Sender.SendNotification(message.Endpoint, message.Payload);
    }

    Task IHostedService.StartAsync(CancellationToken cancellationToken) =>
        _serviceBusProcessor.StartProcessingAsync(cancellationToken);

    Task IHostedService.StopAsync(CancellationToken cancellationToken) =>
        _serviceBusProcessor.StopProcessingAsync(cancellationToken);

    private class ApplicationPropertiesKeys
    {
        public const string Endpoint = "Endpoint";
        public const string RetryNumber = "RetryNumber";
    }
}

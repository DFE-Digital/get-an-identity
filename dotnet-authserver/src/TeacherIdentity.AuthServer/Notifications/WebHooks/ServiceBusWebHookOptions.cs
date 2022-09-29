using System.ComponentModel.DataAnnotations;

namespace TeacherIdentity.AuthServer.Notifications.WebHooks;

public class ServiceBusWebHookOptions
{
    [Required]
    public required string QueueName { get; init; }
}

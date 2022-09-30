using System.ComponentModel.DataAnnotations;

namespace TeacherIdentity.AuthServer.Notifications.WebHooks;

public class WebHookOptions
{
    [Required]
    public int WebHooksCacheDurationSeconds { get; set; }
}

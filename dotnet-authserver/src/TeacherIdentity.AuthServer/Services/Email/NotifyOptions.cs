using System.ComponentModel.DataAnnotations;

namespace TeacherIdentity.AuthServer.Services.Email;

public class NotifyOptions
{
    [Required]
    public required string ApiKey { get; set; }

    public bool ApplyDomainFiltering { get; set; }

    public string[] DomainAllowList { get; set; } = Array.Empty<string>();

    public string? NoSendApiKey { get; set; }
}

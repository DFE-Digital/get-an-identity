using System.ComponentModel.DataAnnotations;

namespace TeacherIdentity.AuthServer;

public class FindALostTrnIntegrationOptions
{
    [Required]
    public string HandoverEndpoint { get; set; } = null!;

    [Required]
    public string SharedKey { get; set; } = null!;
}

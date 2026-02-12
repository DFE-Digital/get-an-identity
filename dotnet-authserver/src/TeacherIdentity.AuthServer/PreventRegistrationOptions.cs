using System.ComponentModel.DataAnnotations;

namespace TeacherIdentity.AuthServer;

public class PreventRegistrationOptions
{
    [Required] public required List<PreventRegistrationOptionsClientRedirect> ClientRedirects { get; set; } = new();
}

public class PreventRegistrationOptionsClientRedirect
{
    [Required]
    public required string ClientId { get; set; }
    [Required]
    public required string RedirectUri { get; set; }
}

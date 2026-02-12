using System.ComponentModel.DataAnnotations;

namespace TeacherIdentity.AuthServer;

public class PreventRegistrationOptions
{
    [Required]
    public List<PreventRegistrationOptionsClientRedirect> ClientRedirects { get; set; }
}

public class PreventRegistrationOptionsClientRedirect
{
    [Required]
    public required string ClientId { get; set; }
    [Required]
    public required string RedirectUri { get; set; }
}

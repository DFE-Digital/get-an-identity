using System.ComponentModel.DataAnnotations;

namespace TeacherIdentity.AuthServer.Services.TrnLookup;

public class FindALostTrnIntegrationOptions
{
    [Required]
    public string HandoverEndpoint { get; set; } = null!;

    [Required]
    public string SharedKey { get; set; } = null!;

    [Required]
    public bool EnableStubEndpoints { get; set; }
}

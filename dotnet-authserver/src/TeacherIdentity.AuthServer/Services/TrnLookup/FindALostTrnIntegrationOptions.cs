using System.ComponentModel.DataAnnotations;

namespace TeacherIdentity.AuthServer.Services.TrnLookup;

public class FindALostTrnIntegrationOptions
{
    [Required]
    public required string HandoverEndpoint { get; set; }

    [Required]
    public required string SharedKey { get; set; }

    [Required]
    public required bool EnableStubEndpoints { get; set; }

    [Required]
    public bool UseNewTrnLookupJourney { get; set; }
}

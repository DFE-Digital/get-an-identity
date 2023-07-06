using System.ComponentModel.DataAnnotations;

namespace TeacherIdentity.AuthServer.Services.TrnTokens;

public class TrnTokenOptions
{
    [Required]
    public required int TokenLifetimeDays { get; set; }
}

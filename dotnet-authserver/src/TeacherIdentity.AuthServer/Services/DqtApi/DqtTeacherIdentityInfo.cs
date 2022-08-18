using System.Text.Json.Serialization;

namespace TeacherIdentity.AuthServer.Services.DqtApi;

public class DqtTeacherIdentityInfo
{
    [JsonPropertyName("TsPersonId")]
    public Guid UserId { get; set; }

    public string Trn { get; set; } = null!;
}

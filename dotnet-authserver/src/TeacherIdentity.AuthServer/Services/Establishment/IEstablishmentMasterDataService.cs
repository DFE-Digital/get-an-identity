namespace TeacherIdentity.AuthServer.Services.Establishment;

public interface IEstablishmentMasterDataService
{
    IAsyncEnumerable<string?> GetEstablishmentWebsites();
}

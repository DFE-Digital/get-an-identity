using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;

namespace TeacherIdentity.AuthServer;

public sealed record ClientRedirectInfo
{
    public const string QueryParameterName = "cri";

    private readonly string _encoded;

    public ClientRedirectInfo(IDataProtector dataProtector, string clientId, string redirectUri)
        : this(Encode(clientId, redirectUri, dataProtector), clientId, redirectUri)
    {
    }

    private ClientRedirectInfo(string encoded, string clientId, string redirectUri)
    {
        ClientId = clientId;
        RedirectUri = redirectUri;
        _encoded = encoded;
    }

    public string ClientId { get; }
    public string RedirectUri { get; }

    public static bool TryDecode(string encoded, IDataProtector dataProtector, [NotNullWhen(true)] out ClientRedirectInfo? clientRedirectInfo)
    {
        string unprotected;
        try
        {
            unprotected = dataProtector.Unprotect(encoded);
        }
        catch (CryptographicException)
        {
            clientRedirectInfo = default;
            return false;
        }

        var asQueryParams = QueryHelpers.ParseQuery(unprotected);
        var clientId = asQueryParams[nameof(ClientId)].ToString();
        var redirectUri = asQueryParams[nameof(RedirectUri)].ToString();

        clientRedirectInfo = new ClientRedirectInfo(encoded, clientId, redirectUri);
        return true;
    }

    private static string Encode(string clientId, string redirectUri, IDataProtector dataProtector)
    {
        var asQueryParams = QueryString
           .Create(nameof(ClientId), clientId)
           .Add(nameof(RedirectUri), redirectUri)
           .Value!;

        return dataProtector.Protect(asQueryParams);
    }

    public override string ToString() => _encoded;

    public string ToQueryParam() => $"{QueryParameterName}={Uri.EscapeDataString(_encoded)}";
}

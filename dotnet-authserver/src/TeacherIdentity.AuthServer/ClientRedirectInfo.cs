using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;

namespace TeacherIdentity.AuthServer;

public sealed record ClientRedirectInfo
{
    public const string QueryParameterName = "cri";

    private readonly string _encoded;

    public ClientRedirectInfo(IDataProtector dataProtector, string clientId, string redirectUri, string signOutUri)
        : this(Encode(clientId, redirectUri, signOutUri, dataProtector), clientId, redirectUri, signOutUri)
    {
    }

    private ClientRedirectInfo(string encoded, string clientId, string redirectUri, string signOutUri)
    {
        ClientId = clientId;
        RedirectUri = redirectUri;
        SignOutUri = signOutUri;
        _encoded = encoded;
    }

    public string ClientId { get; }
    public string RedirectUri { get; }
    public string SignOutUri { get; }

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
        var signOutUri = asQueryParams[nameof(SignOutUri)].ToString();

        clientRedirectInfo = new ClientRedirectInfo(encoded, clientId, redirectUri, signOutUri);
        return true;
    }

    private static string Encode(string clientId, string redirectUri, string signOutUri, IDataProtector dataProtector)
    {
        var asQueryParams = QueryString
           .Create(nameof(ClientId), clientId)
           .Add(nameof(RedirectUri), redirectUri)
           .Add(nameof(SignOutUri), signOutUri)
           .Value!;

        return dataProtector.Protect(asQueryParams);
    }

    public override string ToString() => _encoded;

    public string ToQueryParam() => $"{QueryParameterName}={Uri.EscapeDataString(_encoded)}";
}

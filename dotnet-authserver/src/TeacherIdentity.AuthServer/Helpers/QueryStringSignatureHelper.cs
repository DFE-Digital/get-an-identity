using System.Security.Cryptography;
using System.Text;
using Flurl;

namespace TeacherIdentity.AuthServer.Helpers;

public class QueryStringSignatureHelper
{
    private const string SignatureParameterName = "sig";

    private readonly byte[] _keyBytes;

    public QueryStringSignatureHelper(string key)
    {
        _keyBytes = Encoding.UTF8.GetBytes(key);
    }

    public Url AppendSignature(Url url)
    {
        var sig = CalculateSignature(url);
        return url.SetQueryParam(SignatureParameterName, sig);
    }

    public bool VerifySignature(Url url)
    {
        var sig = CalculateSignature(url);
        var provided = url.QueryParams.SingleOrDefault(qp => qp.Name == SignatureParameterName).Value?.ToString();
        return sig == provided;
    }

    private string CalculateSignature(Url url)
    {
        var canonicalUrl = new Url(url.Path + "?" + url.Query)
            .RemoveQueryParam(SignatureParameterName)
            .ToString()
            .TrimEnd('?');

        var canonicalizedValuesBytes = Encoding.UTF8.GetBytes(canonicalUrl);

        var hashBytes = HMACSHA256.HashData(_keyBytes, canonicalizedValuesBytes);
        var hash = Convert.ToHexString(hashBytes);

        return hash;
    }
}

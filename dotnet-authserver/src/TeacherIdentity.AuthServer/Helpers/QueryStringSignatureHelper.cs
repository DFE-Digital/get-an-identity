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

    public Url AppendSignature(Url url, string[] parameterNames)
    {
        var sig = CalculateSignature(url, parameterNames);
        return url.SetQueryParam(SignatureParameterName, sig);
    }

    public bool VerifySignature(Url url, string[] parameterNames)
    {
        var sig = CalculateSignature(url, parameterNames);
        return sig == url.QueryParams.SingleOrDefault(qp => qp.Name == SignatureParameterName).Value?.ToString();
    }

    private string CalculateSignature(Url url, string[] parameterNames)
    {
        var canonicalizedValues =
            url.Path + "?" +
            string.Join(
                "&",
                parameterNames.Distinct().Order().Select(n =>
                {
                    var value = url.QueryParams.SingleOrDefault(qp => qp.Name == n).Value?.ToString() ?? string.Empty;
                    return $"{Uri.EscapeDataString(n.ToLower())}={Uri.EscapeDataString(value)}";
                }));

        var canonicalizedValuesBytes = Encoding.UTF8.GetBytes(canonicalizedValues);

        var hashBytes = HMACSHA256.HashData(_keyBytes, canonicalizedValuesBytes);
        var hash = Convert.ToHexString(hashBytes);

        return hash;
    }
}

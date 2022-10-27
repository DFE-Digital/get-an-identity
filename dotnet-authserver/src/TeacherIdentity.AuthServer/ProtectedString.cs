using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

namespace TeacherIdentity.AuthServer;

/// <summary>
/// Contains a string and its encrypted value.
/// </summary>
/// <remarks>
/// This is intended to be used to pass data in query parameters that needs to be tamper-proof and/or hidden from clients.
/// </remarks>
[DebuggerDisplay("{PlainValue}")]
public sealed class ProtectedString
{
    private ProtectedString(string plainValue, string encryptedValue)
    {
        PlainValue = plainValue;
        EncryptedValue = encryptedValue;
    }

    public string PlainValue { get; }

    public string EncryptedValue { get; }

    public static ProtectedString CreateFromPlainValue(string plainValue, IDataProtector dataProtector)
    {
        var encryptedValue = dataProtector.Protect(plainValue);
        return new ProtectedString(plainValue, encryptedValue);
    }

    public static bool TryCreateFromEncryptedValue(
        string encryptedValue,
        IDataProtector dataProtector,
        [NotNullWhen(true)] out ProtectedString? protectedString)
    {
        try
        {
            var plainValue = dataProtector.Unprotect(encryptedValue);
            protectedString = new ProtectedString(plainValue, encryptedValue);
            return true;
        }
        catch (CryptographicException)
        {
            protectedString = default;
            return false;
        }
    }

    public override string ToString() => EncryptedValue;
}

public class ProtectedStringFactory
{
    private readonly IDataProtector _dataProtector;

    public ProtectedStringFactory(IDataProtectionProvider dataProtectionProvider)
    {
        _dataProtector = dataProtectionProvider.CreateProtector(nameof(ProtectedString));
    }

    public ProtectedString CreateFromPlainValue(string plainValue) =>
        ProtectedString.CreateFromPlainValue(plainValue, _dataProtector);

    public bool TryCreateFromEncryptedValue(string encryptedValue, [NotNullWhen(true)] out ProtectedString? protectedString) =>
        ProtectedString.TryCreateFromEncryptedValue(encryptedValue, _dataProtector, out protectedString);
}

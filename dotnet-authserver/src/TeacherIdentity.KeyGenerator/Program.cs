using System.Security.Cryptography;

{
    using var rsa = RSA.Create(keySizeInBits: 2048);
    Console.WriteLine(rsa.ToXmlString(includePrivateParameters: true));
}

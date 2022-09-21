using System.Text;

namespace TeacherIdentity.AuthServer.Services;

public class Redactor
{
    public string? RedactEmail(string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            return string.Empty;
        }

        var parts = email.Split('@');

        if (parts.Length != 2)
        {
            // Weird email format - return unredacted first character only
            return email[0] + new string('*', 4);
        }

        var builder = new StringBuilder();

        var localPart = parts[0];
        builder.Append(localPart[0] + new string('*', 4));

        builder.Append("@");

        // If the domain part has suffix(es), keep them and redact the first part.
        // To keep this somewhat simple we redact parts (the bits between the '.'s) until we hit a part with length of 4 or less.
        // The final part is always unredacted, unless it's the only part.
        // Examples:
        //   education.gov.uk => ****.gov.uk
        //   hotmail.co.uk => ****.co.uk
        //   gmail.com => ****.com
        //   digital.education.gov.uk => ****.gov.uk
        //   foo.digital => ****.digital
        //   foo => ****

        var domainParts = parts[1].Split('.');
        var firstDomainPart = domainParts.First();
        var lastDomainPart = domainParts.Last();

        var unredactedDomainParts = domainParts
            .SkipWhile(part => part == firstDomainPart || (part.Length > 4 && part != lastDomainPart))
            .ToArray();

        builder.Append(new string('*', 4));

        if (unredactedDomainParts.Length > 0)
        {
            builder.Append(".");
            builder.Append(string.Join(".", unredactedDomainParts));
        }

        return builder.ToString();
    }
}

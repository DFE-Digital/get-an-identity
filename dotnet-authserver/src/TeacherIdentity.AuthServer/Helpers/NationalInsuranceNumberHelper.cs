using System.Text.RegularExpressions;

namespace TeacherIdentity.AuthServer.Helpers;

public static partial class NationalInsuranceNumberHelper
{
    public static bool IsValid(string? nino)
    {
        if (nino is null)
        {
            return false;
        }

        var normalized = new string(nino.Where(c => !Char.IsWhiteSpace(c) && c != '-').ToArray());

        return ValidNinoPattern().IsMatch(normalized);
    }

    [GeneratedRegex("^[A-CEGHJ-PR-TW-Za-ceghj-pr-tw-z]{1}[A-CEGHJ-NPR-TW-Za-ceghj-npr-tw-z]{1}[0-9]{6}[A-DFMa-dfm]{0,1}$")]
    private static partial Regex ValidNinoPattern();
}

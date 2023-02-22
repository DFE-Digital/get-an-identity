using System.Text.RegularExpressions;

namespace TeacherIdentity.AuthServer.Helpers;

public static class PhoneHelper
{
    public static string FormatMobileNumber(string mobileNumber)
    {
        mobileNumber = Regex.Replace(mobileNumber, @"^[^\d\+]|(?<=.)[^\d]", "");

        if (mobileNumber.StartsWith("00"))
        {
            mobileNumber = "+" + mobileNumber.Substring(2);
        }

        if (mobileNumber.StartsWith("+44"))
        {
            mobileNumber = "0" + mobileNumber.Substring(3);
        }

        return mobileNumber;
    }
}

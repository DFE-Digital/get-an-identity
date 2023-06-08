namespace TeacherIdentity.AuthServer.Services.UserVerification;

public class PinValidator
{
    public string? ValidateCode(string? code)
    {
        if (string.IsNullOrEmpty(code))
        {
            return "Enter a security code";
        }
        else if (!code.All(c => c >= '0' && c <= '9'))
        {
            return "The code must be 5 numbers";
        }
        else if (code.Length < 5)
        {
            return "You’ve not entered enough numbers, the code must be 5 numbers";
        }
        else if (code.Length > 5)
        {
            return "You’ve entered too many numbers, the code must be 5 numbers";
        }

        return null;
    }
}

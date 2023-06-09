using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using TeacherIdentity.AuthServer.Helpers;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class RequiredIfTrueAttribute : RequiredAttribute
{
    private string PropertyName { get; }
    private string? TargetValue { get; }

    public RequiredIfTrueAttribute(string propertyName, string? targetValue = null)
    {
        PropertyName = propertyName;
        TargetValue = targetValue;
    }

    protected override ValidationResult IsValid(object? value, ValidationContext context)
    {
        object instance = context.ObjectInstance;
        Type type = instance.GetType();

        var propertyValue = type.GetProperty(PropertyName)?.GetValue(instance)?.ToString();
        bool isRequired;

        if (TargetValue is null)
        {
            bool.TryParse(propertyValue, out isRequired);
        }
        else
        {
            isRequired = propertyValue == TargetValue;
        }

        return isRequired && !base.IsValid(value) ? new ValidationResult(ErrorMessage) : ValidationResult.Success!;
    }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class RegexIfTrueAttribute : RegularExpressionAttribute
{
    private string PropertyName { get; }

    public RegexIfTrueAttribute(string propertyName, [StringSyntax(StringSyntaxAttribute.Regex)] string pattern)
        : base(pattern)
    {
        PropertyName = propertyName;
    }

    protected override ValidationResult IsValid(object? value, ValidationContext context)
    {
        object instance = context.ObjectInstance;
        Type type = instance.GetType();

        bool.TryParse(type.GetProperty(PropertyName)?.GetValue(instance)?.ToString(), out bool isRequired);
        return isRequired && !base.IsValid(value) ? new ValidationResult(ErrorMessage) : ValidationResult.Success!;
    }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class IsValidDateOfBirthAttribute : RangeAttribute
{
    public IsValidDateOfBirthAttribute()
        : base(typeof(DateOnly), new DateTime(1900, 1, 1).ToShortDateString(), DateTime.Now.ToShortDateString())
    {
    }

    public override bool IsValid(object? value)
    {
        if (value is DateOnly dateValue)
        {
            var minimumDate = DateOnly.Parse(Minimum.ToString()!);
            var maximumDate = DateOnly.Parse(Maximum.ToString()!);

            if (dateValue < minimumDate)
            {
                ErrorMessage = "Enter a valid date of birth";
                return false;
            }

            if (dateValue > maximumDate)
            {
                ErrorMessage = "Your date of birth must be in the past";
                return false;
            }
        }

        return base.IsValid(value);
    }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class FileSizeAttribute : ValidationAttribute
{
    private readonly int _maxFileSize;

    public FileSizeAttribute(int maxFileSize)
    {
        _maxFileSize = maxFileSize;
    }

    protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
    {
        if (value is IFormFile file && file.Length > _maxFileSize)
        {
            return new ValidationResult(ErrorMessage);
        }

        return ValidationResult.Success!;
    }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class FileExtensionsAttribute : ValidationAttribute
{
    private List<string> AllowedExtensions { get; set; }

    public FileExtensionsAttribute(params string[] fileExtensions)
    {
        AllowedExtensions = fileExtensions.ToList();
    }

    public override bool IsValid(object? value)
    {
        if (value is IFormFile file)
        {
            var fileName = file.FileName;

            return AllowedExtensions.Any(extension => fileName.EndsWith(extension));
        }

        return true;
    }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class MobilePhoneAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        return value is string str && MobileNumber.TryParse(str, out _);
    }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class EmailAddressAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        return value is string str && EmailAddress.TryParse(str, out _);
    }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class EmailAddressIfTrueAttribute : EmailAddressAttribute
{
    private string PropertyName { get; }

    public EmailAddressIfTrueAttribute(string propertyName)
    {
        PropertyName = propertyName;
    }

    protected override ValidationResult IsValid(object? value, ValidationContext context)
    {
        object instance = context.ObjectInstance;
        Type type = instance.GetType();

        bool.TryParse(type.GetProperty(PropertyName)?.GetValue(instance)?.ToString(), out bool isRequired);
        return isRequired && !base.IsValid(value) ? new ValidationResult(ErrorMessage) : ValidationResult.Success!;
    }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class NationalInsuranceNumber : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        return value is string str && NationalInsuranceNumberHelper.IsValid(str);
    }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class StringLengthIfTrueAttribute : StringLengthAttribute
{
    private string PropertyName { get; }
    private string? TargetValue { get; }

    public StringLengthIfTrueAttribute(string propertyName, int maximumLength, string? targetValue = null) : base(maximumLength)
    {
        PropertyName = propertyName;
        TargetValue = targetValue;
    }

    protected override ValidationResult IsValid(object? value, ValidationContext context)
    {
        object instance = context.ObjectInstance;
        Type type = instance.GetType();

        var propertyValue = type.GetProperty(PropertyName)?.GetValue(instance)?.ToString();
        bool isRequired;

        if (TargetValue is null)
        {
            bool.TryParse(propertyValue, out isRequired);
        }
        else
        {
            isRequired = propertyValue == TargetValue;
        }

        return isRequired && !base.IsValid(value) ? new ValidationResult(ErrorMessage) : ValidationResult.Success!;
    }
}

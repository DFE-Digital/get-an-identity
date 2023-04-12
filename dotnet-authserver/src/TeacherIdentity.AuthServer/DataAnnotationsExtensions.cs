using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class RequiredIfTrueAttribute : RequiredAttribute
{
    private string PropertyName { get; }

    public RequiredIfTrueAttribute(string propertyName)
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
public class IsPastDateAttribute : RangeAttribute
{
    public IsPastDateAttribute(Type type)
        : base(type, DateTime.MinValue.ToShortDateString(), DateTime.Now.ToShortDateString())
    {
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

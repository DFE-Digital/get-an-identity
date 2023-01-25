using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

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
using FluentValidation;

namespace TeacherIdentity.AuthServer.Api.Validation;

public static class ValidationExtensions
{
    public static IRuleBuilderOptions<T, string?> IdentityEmailAddress<T>(this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder
            .Must(email => Models.EmailAddress.TryParse(email, out _))
            .WithMessage("Email is not valid.")
            .MaximumLength(Models.EmailAddress.EmailAddressMaxLength)
            .WithMessage($"Email must be {Models.EmailAddress.EmailAddressMaxLength} characters or less.");
    }
}

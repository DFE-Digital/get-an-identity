using FluentValidation;
using TeacherIdentity.AuthServer.Api.V1.Requests;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Api.V1.Validators;

public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(r => r.Body.Email)
            .Cascade(CascadeMode.Stop)
            .EmailAddress()
                .WithMessage("Email is not valid.")
            .MaximumLength(User.EmailAddressMaxLength)
                .WithMessage($"Email must be {User.EmailAddressMaxLength} characters or less.")
            .When(r => r.Body.EmailSet);

        RuleFor(r => r.Body.FirstName)
            .Cascade(CascadeMode.Stop)
            .MaximumLength(User.FirstNameAddressMaxLength)
                .WithMessage($"First name must be {User.FirstNameAddressMaxLength} characters or less.")
            .When(r => r.Body.FirstNameSet);

        RuleFor(r => r.Body.LastName)
            .Cascade(CascadeMode.Stop)
            .MaximumLength(User.LastNameAddressMaxLength)
                .WithMessage($"Last name must be {User.LastNameAddressMaxLength} characters or less.")
            .When(r => r.Body.LastNameSet);
    }
}

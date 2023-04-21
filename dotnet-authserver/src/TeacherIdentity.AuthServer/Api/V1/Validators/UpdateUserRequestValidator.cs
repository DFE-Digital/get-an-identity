using FluentValidation;
using TeacherIdentity.AuthServer.Api.V1.Requests;
using TeacherIdentity.AuthServer.Api.Validation;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Api.V1.Validators;

public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(r => r.Body.Email)
            .IdentityEmailAddress()
            .When(r => r.Body.EmailSet);

        RuleFor(r => r.Body.FirstName)
            .Cascade(CascadeMode.Stop)
            .MaximumLength(User.FirstNameMaxLength)
                .WithMessage($"First name must be {User.FirstNameMaxLength} characters or less.")
            .When(r => r.Body.FirstNameSet);

        RuleFor(r => r.Body.LastName)
            .Cascade(CascadeMode.Stop)
            .MaximumLength(User.LastNameMaxLength)
                .WithMessage($"Last name must be {User.LastNameMaxLength} characters or less.")
            .When(r => r.Body.LastNameSet);
    }
}

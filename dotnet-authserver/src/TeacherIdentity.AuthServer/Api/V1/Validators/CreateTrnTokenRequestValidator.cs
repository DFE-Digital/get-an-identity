using FluentValidation;
using TeacherIdentity.AuthServer.Api.V1.Requests;
using TeacherIdentity.AuthServer.Api.Validation;

namespace TeacherIdentity.AuthServer.Api.V1.Validators;

public class CreateTrnTokenRequestValidator : AbstractValidator<CreateTrnTokenRequest>
{
    public CreateTrnTokenRequestValidator()
    {
        RuleFor(r => r.Trn)
            .Must(trn => trn is null || (trn.Length == 7 && trn.All(Char.IsAsciiDigit)))
            .WithMessage("TRN is not valid.");

        RuleFor(r => r.Email)
            .Cascade(CascadeMode.Stop)
            .IdentityEmailAddress();
    }
}

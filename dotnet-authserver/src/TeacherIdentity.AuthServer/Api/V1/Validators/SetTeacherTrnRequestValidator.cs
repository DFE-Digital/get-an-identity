using FluentValidation;
using TeacherIdentity.AuthServer.Api.V1.Requests;

namespace TeacherIdentity.AuthServer.Api.V1.Validators;

public class SetTeacherTrnRequestValidator : AbstractValidator<SetTeacherTrnRequest>
{
    public SetTeacherTrnRequestValidator()
    {
        RuleFor(r => r.Body.Trn)
            .NotEmpty()
                .WithMessage("TRN is required.")
            .Must(trn => trn?.Length == 7 && trn.All(c => c >= '0' && c <= '9'))
                .WithMessage("TRN is not valid.");
    }
}

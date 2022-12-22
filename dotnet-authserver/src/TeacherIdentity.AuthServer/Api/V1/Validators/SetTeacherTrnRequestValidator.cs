using FluentValidation;
using TeacherIdentity.AuthServer.Api.V1.Requests;

namespace TeacherIdentity.AuthServer.Api.V1.Validators;

public class SetTeacherTrnRequestValidator : AbstractValidator<SetTeacherTrnRequest>
{
    public SetTeacherTrnRequestValidator()
    {
        RuleFor(r => r.Body.Trn)
            .Must(trn => trn is null || (trn.Length == 7 && trn.All(Char.IsAsciiDigit)))
                .WithMessage("TRN is not valid.");
    }
}

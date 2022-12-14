using FluentValidation;
using TeacherIdentity.AuthServer.Api.V1.Requests;

namespace TeacherIdentity.AuthServer.Api.V1.Validators;

public class GetAllUsersRequestValidator : AbstractValidator<GetAllUsersRequest>
{
    public GetAllUsersRequestValidator()
    {
        RuleFor(r => r.PageSize)
           .InclusiveBetween(1, 100)
           .When(x => x.PageSize.HasValue)
           .WithMessage("Page size must be between 1-100");

        RuleFor(r => r.PageNumber)
            .Must(x => x > 0)
            .When(x => x.PageNumber.HasValue)
            .WithMessage("Page must be greater than or equal to 1");
    }
}

using FluentValidation;
using MediatR;
using MediatR.Pipeline;

namespace TeacherIdentity.AuthServer.Api.V1.Handlers;

public class ValidatorBehavior<TRequest> : IRequestPreProcessor<TRequest>
    where TRequest : IRequest
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidatorBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task Process(TRequest request, CancellationToken cancellationToken)
    {
        foreach (var validator in _validators)
        {
            await validator.ValidateAndThrowAsync(request, cancellationToken);
        }
    }
}

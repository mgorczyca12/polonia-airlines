using DispatchR.Abstractions.Send;
using FluentValidation;

namespace Shared.Application.Pipelines;

public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, ValueTask<TResponse>>
    where TRequest : class, IRequest<TRequest, ValueTask<TResponse>>
{
    public required IRequestHandler<TRequest, ValueTask<TResponse>> NextPipeline { get; set; }

    public async ValueTask<TResponse> Handle(TRequest request, CancellationToken cancellationToken)
    {
        var validationContext = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(
            validators.Select(validator => validator.ValidateAsync(validationContext, cancellationToken)));
        var failures = results.SelectMany(result => result.Errors).Where(error => error is not null).ToArray();

        if (failures.Length > 0)
            throw new ValidationException(failures);

        return await NextPipeline.Handle(request, cancellationToken);
    }
}
namespace XanhNow.Security.Application.Abstractions.Validation;

public interface IValidator<in TRequest>
{
    ValueTask<IReadOnlyCollection<ValidationFailure>> ValidateAsync(TRequest request, CancellationToken cancellationToken);
}

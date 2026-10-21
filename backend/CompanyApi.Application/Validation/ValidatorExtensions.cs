using FluentValidation;

namespace CompanyApi.Application.Validation;

public static class ValidatorExtensions
{
    /// <summary>Throws a <see cref="ValidationException"/> (mapped to HTTP 400) with all messages joined.</summary>
    public static void ValidateOrThrow<T>(this IValidator<T> validator, T instance)
    {
        var result = validator.Validate(instance);
        if (!result.IsValid)
            throw new Services.ValidationException(string.Join(" ", result.Errors.Select(e => e.ErrorMessage)));
    }
}

using CompanyApi.Application.Dtos;
using FluentValidation;

namespace CompanyApi.Application.Validation;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Email is required.")
            .MaximumLength(254).WithMessage("Email must have at most 254 characters.")
            .EmailAddress().WithMessage("Email is not valid.");

        RuleFor(x => x.Password).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must have at least 8 characters.")
            .MaximumLength(128).WithMessage("Password must have at most 128 characters.");
    }
}

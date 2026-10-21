using CompanyApi.Application.Dtos;
using FluentValidation;

namespace CompanyApi.Application.Validation;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().MaximumLength(254).WithMessage("Email is required (max 254 characters).");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required.");
    }
}

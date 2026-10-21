using CompanyApi.Application.Dtos;
using FluentValidation;

namespace CompanyApi.Application.Validation;

public class CreateCompanyRequestValidator : AbstractValidator<CreateCompanyRequest>
{
    public CreateCompanyRequestValidator()
    {
        RuleFor(x => x.Name).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Company name is required.")
            .Must(n => n!.Trim().Length >= 3).WithMessage("Company name must have at least 3 characters.")
            .MaximumLength(200).WithMessage("Company name must have at most 200 characters.");

        RuleFor(x => x.WebsiteUrl).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Website URL is required.")
            .MaximumLength(2048).WithMessage("Website URL is too long.")
            .Must(BeAbsoluteWebUrl).WithMessage("Website URL must be a valid absolute URL, e.g. https://www.example.com.");
    }

    private static bool BeAbsoluteWebUrl(string? url) =>
        Uri.TryCreate(url?.Trim(), UriKind.Absolute, out var uri)
        && uri.Scheme is "http" or "https"
        && uri.Host.Contains('.');
}

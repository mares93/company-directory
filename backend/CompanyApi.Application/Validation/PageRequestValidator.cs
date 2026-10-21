using CompanyApi.Application.Dtos;
using FluentValidation;

namespace CompanyApi.Application.Validation;

public class PageRequestValidator : AbstractValidator<PageRequest>
{
    private static readonly string[] Sorts = ["relevance", "name", "newest"];

    public PageRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("Page must be 1 or greater.");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");
        RuleFor(x => x.Sort)
            .Must(s => s is null || Sorts.Contains(s, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Sort must be one of: relevance, name, newest.");
    }
}

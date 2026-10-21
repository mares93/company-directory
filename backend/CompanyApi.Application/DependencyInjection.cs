using CompanyApi.Application.Services;
using CompanyApi.Application.Validation;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace CompanyApi.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services) => services
        .AddScoped<ICompanyService, CompanyService>()
        .AddScoped<IAuthService, AuthService>()
        .AddValidatorsFromAssemblyContaining<CreateCompanyRequestValidator>();
}

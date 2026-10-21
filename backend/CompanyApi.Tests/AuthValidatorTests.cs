using CompanyApi.Application.Dtos;
using CompanyApi.Application.Validation;

namespace CompanyApi.Tests;

public class AuthValidatorTests
{
    [Theory]
    [InlineData("a@b.com", "password123", true)]
    [InlineData("not-an-email", "password123", false)]
    [InlineData("a@b.com", "short", false)]
    [InlineData(null, "password123", false)]
    [InlineData("a@b.com", null, false)]
    public void Register_rules(string? email, string? password, bool valid) =>
        Assert.Equal(valid, new RegisterRequestValidator().Validate(new RegisterRequest(email, password)).IsValid);
}

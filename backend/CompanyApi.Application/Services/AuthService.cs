using CompanyApi.Application.Dtos;
using CompanyApi.Application.Persistence;
using CompanyApi.Application.Validation;
using CompanyApi.Domain;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CompanyApi.Application.Services;

public class AuthService(
    IAppDbContext db,
    IPasswordHasher hasher,
    ITokenService tokens,
    IValidator<RegisterRequest> registerValidator,
    IValidator<LoginRequest> loginValidator,
    ILogger<AuthService> logger) : IAuthService
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        registerValidator.ValidateOrThrow(request);
        var email = Normalize(request.Email);

        // The explicit check gives a clear error without paying for a hash; the unique index on Email holds under concurrency.
        if (await db.Users.AnyAsync(u => u.Email == email))
            throw EmailTaken();

        var user = new User(Guid.NewGuid(), email, hasher.Hash(request.Password!), DateTime.UtcNow);
        db.Users.Add(user);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            if (await db.Users.AnyAsync(u => u.Email == email)) throw EmailTaken();
            throw;
        }

        logger.LogInformation("Registered user {Email}", email);
        return tokens.Create(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        loginValidator.ValidateOrThrow(request);
        var email = Normalize(request.Email);
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
        // Same message and same cost for unknown email and wrong password: do not reveal which accounts exist.
        var passwordOk = hasher.Verify(request.Password!, user?.PasswordHash);
        if (user is null || !passwordOk)
        {
            logger.LogWarning("Failed login for {Email}", email);
            throw new InvalidCredentialsException("Invalid email or password.");
        }

        logger.LogInformation("User {Email} logged in", email);
        return tokens.Create(user);
    }

    private static string Normalize(string? email) => (email ?? "").Trim().ToLowerInvariant();

    private static ConflictException EmailTaken() => new("An account with this email already exists.");
}

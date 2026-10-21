namespace CompanyApi.Domain;

public record User(Guid Id, string Email, string PasswordHash, DateTime CreatedAtUtc);

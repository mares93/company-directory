namespace CompanyApi.Application.Dtos;

public record AuthResponse(string Token, string Email, DateTime ExpiresAtUtc);

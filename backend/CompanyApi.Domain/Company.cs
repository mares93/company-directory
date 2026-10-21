namespace CompanyApi.Domain;

// Internal model: never leaves the service layer (controllers only see DTOs).
public record Company(Guid Id, string Name, string WebsiteUrl, string Domain, string CreatedBy, DateTime CreatedAtUtc);

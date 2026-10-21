namespace CompanyApi.Application.Dtos;

public record CompanyResponse(Guid Id, string Name, string WebsiteUrl, string Domain, string CreatedBy, DateTime CreatedAtUtc);

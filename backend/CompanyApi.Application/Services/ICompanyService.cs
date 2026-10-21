using CompanyApi.Application.Dtos;

namespace CompanyApi.Application.Services;

public interface ICompanyService
{
    Task<CompanyResponse> CreateAsync(CreateCompanyRequest request, string createdBy);
    Task<CompanyResponse> GetByIdAsync(Guid id);
    Task<PagedResponse<CompanyResponse>> GetAllAsync(string? name, string? domain, PageRequest paging);
    Task<PagedResponse<CompanySearchResult>> SearchAsync(string query, string? domain, PageRequest paging);
}

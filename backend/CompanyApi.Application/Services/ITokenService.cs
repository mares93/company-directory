using CompanyApi.Application.Dtos;
using CompanyApi.Domain;

namespace CompanyApi.Application.Services;

public interface ITokenService
{
    AuthResponse Create(User user);
}

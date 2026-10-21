namespace CompanyApi.Application.Services;

public interface IPasswordHasher
{
    string Hash(string password);
    /// <summary>A null hash (unknown user) still costs the same time, so response time does not reveal which emails exist.</summary>
    bool Verify(string password, string? hash);
}

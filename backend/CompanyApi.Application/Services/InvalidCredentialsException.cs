namespace CompanyApi.Application.Services;

public class InvalidCredentialsException(string message) : Exception(message);

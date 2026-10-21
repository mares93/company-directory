namespace CompanyApi.Application.Services;

public class NotFoundException(string message) : Exception(message);

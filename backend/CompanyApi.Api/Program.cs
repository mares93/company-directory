using System.Text;
using System.Threading.RateLimiting;
using CompanyApi.Application;
using CompanyApi.Application.Dtos;
using CompanyApi.Application.Services;
using CompanyApi.Infrastructure;
using CompanyApi.Infrastructure.Security;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddProblemDetails();

// Fail at startup, not on the first request, when the signing key is missing or too short.
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()
    ?? throw new InvalidOperationException("Missing 'Jwt' configuration section.");
if (jwt.Key.Length < 32) throw new InvalidOperationException("Jwt:Key must be at least 32 characters.");

builder.Services.AddAuthentication().AddJwtBearer(o =>
{
    o.MapInboundClaims = false; // keep claim names as issued ("email", "sub")
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = jwt.Issuer,
        ValidAudience = jwt.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
        ClockSkew = TimeSpan.Zero
    };
});
builder.Services.AddAuthorization();

// register/login are public and cost CPU (password hashing): limit attempts per client address.
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("auth", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = ctx.RequestServices.GetRequiredService<IConfiguration>().GetValue("RateLimit:AuthPerMinute", 10),
            Window = TimeSpan.FromMinutes(1)
        }));
});

builder.Services.AddSwaggerGen(o =>
{
    o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Paste the token returned by /api/auth/login"
    });
    o.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", doc)] = []
    });
});
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(); // http://localhost:5000/swagger
}

// Default user for demos: only configured in appsettings.Development.json. Goes through the normal registration rules.
if (app.Configuration["SeedUser:Email"] is { Length: > 0 } seedEmail)
{
    using var scope = app.Services.CreateScope();
    try
    {
        await scope.ServiceProvider.GetRequiredService<IAuthService>()
            .RegisterAsync(new RegisterRequest(seedEmail, app.Configuration["SeedUser:Password"]));
    }
    catch (ConflictException)
    {
        // already seeded: the data survived a restart
    }
}

app.UseExceptionHandler();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program;

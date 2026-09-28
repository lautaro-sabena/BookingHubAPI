using BookingHubAPI.API.Configuration;
using BookingHubAPI.Infrastructure.Configuration;
using BookingHubAPI.Infrastructure.Data;
using BookingHubAPI.Infrastructure.Repositories;
using BookingHubAPI.Infrastructure.Auth;
using BookingHubAPI.Infrastructure.Services;
using BookingHubAPI.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using AspNetCoreRateLimit;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

var jwtKey = StartupConfigurationValidator.RequireJwtSecretKey(builder.Configuration);
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "BookingHubAPI";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "BookingHubAPI";

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var connectionString = StartupConfigurationValidator.RequireConnectionString(builder.Configuration);
builder.Services.AddDbContext<BookingDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions => 
        npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(30), errorCodesToAdd: new List<string>())));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICompanyRepository, CompanyRepository>();
builder.Services.AddScoped<IServiceRepository, ServiceRepository>();
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();
builder.Services.AddScoped<IWorkingHoursRepository, WorkingHoursRepository>();
builder.Services.AddScoped<IFavoriteRepository, FavoriteRepository>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Behind Render's proxy every request arrives from the proxy IP, so the rate limiter would
// share one counter across all clients. ForwardedHeaders restores the client IP from
// X-Forwarded-For for every consumer. Render publishes no fixed proxy range, so
// ForwardedHeaders:TrustAllProxies (set in render.yaml) trusts the single immediate hop.
// Only enable it where the proxy is the sole ingress; otherwise clients can spoof the header.
builder.Services.AddOptions<ForwardedHeadersOptions>().Configure<IConfiguration>((options, configuration) =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;
    if (configuration.GetValue<bool>("ForwardedHeaders:TrustAllProxies"))
    {
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    }
});

builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
builder.Services.AddInMemoryRateLimiting();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero,
        ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 }
    };
});

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(CorsOriginsResolver.Resolve(builder.Configuration))
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddHealthChecks()
    .AddDbContextCheck<BookingDbContext>();

var app = builder.Build();

// Must run before anything that reads RemoteIpAddress (rate limiting, CORS, auth/audit code).
app.UseForwardedHeaders();

app.UseMiddleware<BookingHubAPI.API.Middleware.ErrorHandlingMiddleware>();

app.UseCors();

app.UseRouting();

// HTTPS redirection is handled by Render's proxy

app.UseIpRateLimiting();

app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
    db.Database.EnsureCreated();
}

app.MapControllers();

app.MapHealthChecks("/health");

app.Run();

public partial class Program { }

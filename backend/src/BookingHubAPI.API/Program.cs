using BookingHubAPI.API.Authentication;
using BookingHubAPI.API.Configuration;
using BookingHubAPI.API.Middleware;
using BookingHubAPI.Application;
using BookingHubAPI.Application.Abstractions;
using BookingHubAPI.Infrastructure.Configuration;
using BookingHubAPI.Infrastructure.Data;
using BookingHubAPI.Infrastructure.Repositories;
using BookingHubAPI.Infrastructure.Auth;
using BookingHubAPI.Infrastructure.Services;
using BookingHubAPI.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using AspNetCoreRateLimit;

// Keeps DateTime mapped to "timestamp without time zone" - the schema production was created with. The design-time
// factory (BookingDbContextFactory) sets the same switch. The schema is NOT created here: apply the EF migrations
// (see backend/README.md, "Database migrations runbook").
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Do not advertise the server software.
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

var jwtKey = StartupConfigurationValidator.RequireJwtSecretKey(builder.Configuration);
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "BookingHubAPI";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "BookingHubAPI";

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();

var connectionString = StartupConfigurationValidator.RequireConnectionString(builder.Configuration);
builder.Services.AddDbContext<BookingDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions => 
        npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(30), errorCodesToAdd: new List<string>())));

builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICompanyRepository, CompanyRepository>();
builder.Services.AddScoped<IServiceRepository, ServiceRepository>();
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();
builder.Services.AddScoped<IWorkingHoursRepository, WorkingHoursRepository>();
builder.Services.AddScoped<IFavoriteRepository, FavoriteRepository>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddScoped<INotificationService, NotificationService>();

builder.Services.AddApplication();

// The session JWT travels in an httpOnly cookie (see AuthController); Authorization: Bearer keeps working.
builder.Services.Configure<SessionCookieOptions>(builder.Configuration.GetSection(SessionCookieOptions.SectionName));
builder.Services.AddSingleton<SessionCookie>();

// Behind Render every request arrives from the proxy IP, so client IPs come from X-Forwarded-For (see
// UseForwardedClientIp below for the trusted hop counts and the FRONTEND_PROXY_KEY scheme).

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

    // Browsers authenticate with the session cookie; an explicit Authorization header (API clients) always wins.
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            if (string.IsNullOrEmpty(context.Request.Headers.Authorization))
            {
                context.Token = context.HttpContext.RequestServices.GetRequiredService<SessionCookie>().Read(context.Request);
            }
            return Task.CompletedTask;
        }
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

// Fail fast on an unmigrated database instead of serving 500s. Default: on outside Development.
var failOnPendingMigrations = app.Configuration.GetValue("Database:FailOnPendingMigrations", !app.Environment.IsDevelopment());
using (var scope = app.Services.CreateScope())
{
    await DatabaseSchemaCheck.EnsureUpToDateAsync(
        scope.ServiceProvider.GetRequiredService<BookingDbContext>(),
        app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("BookingHubAPI.Startup"),
        failOnPendingMigrations);
}

// Must run before anything that reads RemoteIpAddress (rate limiting, CORS, auth/audit code).
app.UseForwardedClientIp(app.Configuration);

app.UseSecurityHeaders();

// Render terminates TLS; the forwarded proto above makes the request look like HTTPS so the header is emitted.
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseMiddleware<BookingHubAPI.API.Middleware.ErrorHandlingMiddleware>();

// Gives bodiless error responses (401/403 from auth, unmatched routes, ...) a problem+json body.
app.UseStatusCodePages();

app.UseCors();

app.UseRouting();

// HTTPS redirection is handled by Render's proxy

app.UseIpRateLimiting();

// After routing (reads endpoint metadata), before the cookie is turned into an identity.
app.UseMiddleware<CsrfHeaderMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health");

app.Run();

public partial class Program { }

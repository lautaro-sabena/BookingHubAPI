using BookingHubAPI.API;
using BookingHubAPI.Application.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using BookingHubAPI.Infrastructure.Data;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace BookingHubAPI.IntegrationTests;

public class AuthControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AuthControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<BookingDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<BookingDbContext>(options =>
                    options.UseInMemoryDatabase("TestDb_" + Guid.NewGuid()));
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidData_ShouldReturnToken()
    {
        var request = new RegisterRequest("newuser@test.com", "Password123!", "Customer");

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<TokenResponse>();
        content.Should().NotBeNull();
        content!.Token.Should().NotBeEmpty();
        content.Email.Should().Be("newuser@test.com");
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ShouldReturnBadRequest()
    {
        var request = new RegisterRequest("duplicate@test.com", "Password123!", "Customer");

        await _client.PostAsJsonAsync("/api/auth/register", request);
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithInvalidRole_ShouldReturnBadRequest()
    {
        var request = new RegisterRequest("user@test.com", "Password123!", "InvalidRole");

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnToken()
    {
        var registerRequest = new RegisterRequest("logintest@test.com", "Password123!", "Customer");
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginRequest = new LoginRequest("logintest@test.com", "Password123!");
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<TokenResponse>();
        content.Should().NotBeNull();
        content!.Token.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Login_WithWrongPassword_ShouldReturnUnauthorized()
    {
        var registerRequest = new RegisterRequest("wrongpass@test.com", "CorrectPassword123!", "Customer");
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginRequest = new LoginRequest("wrongpass@test.com", "WrongPassword");
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithNonExistentUser_ShouldReturnUnauthorized()
    {
        var loginRequest = new LoginRequest("nonexistent@test.com", "Password123!");
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

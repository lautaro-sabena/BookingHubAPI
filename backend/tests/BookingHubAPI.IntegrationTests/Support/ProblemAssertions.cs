using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;

namespace BookingHubAPI.IntegrationTests.Support;

/// <summary>Helpers for asserting the RFC 7807 shape every API error response now shares.</summary>
public static class ProblemAssertions
{
    /// <summary>
    /// Reads the body as a <see cref="ProblemDetails"/>, asserting the problem+json content type
    /// and that the standard members (status, title, traceId) are present.
    /// </summary>
    public static async Task<ProblemDetails> ReadProblemAsync(this HttpResponseMessage response)
    {
        response.Content.Headers.ContentType.Should().NotBeNull();
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be((int)response.StatusCode);
        problem.Title.Should().NotBeNullOrWhiteSpace();
        problem.Extensions.Should().ContainKey("traceId");
        return problem;
    }

    /// <summary>Asserts the response is a problem with the given status and (when given) exact <c>detail</c>.</summary>
    public static async Task<ProblemDetails> ShouldBeProblemAsync(
        this HttpResponseMessage response, HttpStatusCode status, string? detail = null)
    {
        response.StatusCode.Should().Be(status);
        var problem = await response.ReadProblemAsync();
        if (detail is not null)
        {
            problem.Detail.Should().Be(detail);
        }

        return problem;
    }
}

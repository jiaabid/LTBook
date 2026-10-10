using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ToolBook.IntegrationTests.Infrastructure;

public sealed record ToolDto(Guid Id, string Name, bool IsActive, int DefaultSlotMinutes, int TypicalBookingMinutes);

public sealed record BookingDto(
    Guid Id, Guid ToolId, Guid UserId, DateTimeOffset Start, DateTimeOffset End, string Purpose, string Status,
    DateTimeOffset? CheckedInAt, DateTimeOffset? CheckedOutAt, string? CancellationReason);

public sealed record ProblemDto(int Status, string? Title, string? Detail, string? Code, Dictionary<string, string[]>? Errors);

/// <summary>Small helpers for calling the API as different users.</summary>
public static class ApiClient
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    public static async Task<HttpClient> AdminAsync(this ToolBookApiFactory factory) =>
        await factory.LoginAsync(ToolBookApiFactory.AdminEmail, ToolBookApiFactory.AdminPassword);

    /// <summary>Registers a fresh user and returns a client authenticated as that user.</summary>
    public static async Task<HttpClient> NewUserAsync(this ToolBookApiFactory factory, string displayName = "Test User")
    {
        var email = $"user-{Guid.NewGuid():N}@test.local";
        const string password = "User_Test_Pass1!";
        var anonymous = factory.CreateClient();
        var register = await anonymous.PostAsJsonAsync("/api/users/register", new { email, password, displayName });
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);
        return await factory.LoginAsync(email, password);
    }

    public static async Task<HttpClient> LoginAsync(this ToolBookApiFactory factory, string email, string password)
    {
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>Creates a dedicated tool so tests never share a calendar.</summary>
    public static async Task<ToolDto> CreateToolAsync(this HttpClient admin, int defaultSlotMinutes = 60)
    {
        var response = await admin.PostAsJsonAsync("/api/tools", new
        {
            name = $"Test tool {Guid.NewGuid():N}",
            type = "Test",
            location = "Lab 0",
            description = "Created by an integration test.",
            defaultSlotMinutes,
            typicalBookingMinutes = 60,
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ToolDto>(Json))!;
    }

    public static Task<HttpResponseMessage> BookAsync(this HttpClient client, Guid toolId, DateTimeOffset start, DateTimeOffset end, string purpose = "Measurement") =>
        client.PostAsJsonAsync("/api/bookings", new { toolId, start, end, purpose });

    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(Json))!;
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ToolBook.IntegrationTests.Infrastructure;

namespace ToolBook.IntegrationTests;

[Collection(ApiCollection.Name)]
public class AuthAndToolsTests(ToolBookApiFactory factory)
{
    [Fact]
    public async Task Register_then_login_returns_profile_with_user_role()
    {
        var client = await factory.NewUserAsync("Marie Curie");

        var me = await (await client.GetAsync("/api/users/me")).ReadAsync<JsonElement>();

        Assert.Equal("Marie Curie", me.GetProperty("displayName").GetString());
        Assert.Equal(new[] { "User" }, me.GetProperty("roles").EnumerateArray().Select(r => r.GetString()));
    }

    [Fact]
    public async Task Built_in_identity_register_is_disabled()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/register", new { email = "x@test.local", password = "Pass_word1!" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Registration_validation_errors_are_problem_details()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/users/register", new { email = "not-an-email", password = "x", displayName = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.ReadAsync<ProblemDto>();
        Assert.Contains("Email", problem.Errors!.Keys);
        Assert.Contains("DisplayName", problem.Errors!.Keys);
    }

    [Fact]
    public async Task Api_requires_authentication()
    {
        var response = await factory.CreateClient().GetAsync("/api/tools");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Seeded_tools_are_listed()
    {
        var client = await factory.NewUserAsync();

        var tools = await (await client.GetAsync("/api/tools")).ReadAsync<List<ToolDto>>();

        Assert.Contains(tools, t => t.Name == "Scanning Electron Microscope");
        Assert.Contains(tools, t => t.Name == "Probe Station");
    }

    [Fact]
    public async Task Only_admins_can_create_tools()
    {
        var user = await factory.NewUserAsync();

        var response = await user.PostAsJsonAsync("/api/tools", new
        {
            name = "Sneaky tool", type = "x", location = "x", description = "", defaultSlotMinutes = 30, typicalBookingMinutes = 30,
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Health_endpoint_reports_healthy_database()
    {
        var response = await factory.CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }
}

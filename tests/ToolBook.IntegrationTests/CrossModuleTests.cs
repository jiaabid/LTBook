using System.Net;
using ToolBook.IntegrationTests.Infrastructure;

namespace ToolBook.IntegrationTests;

[Collection(ApiCollection.Name)]
public class CrossModuleTests(ToolBookApiFactory factory)
{
    [Fact]
    public async Task Deactivating_a_tool_cancels_its_future_bookings_via_integration_event()
    {
        var admin = await factory.AdminAsync();
        var tool = await admin.CreateToolAsync();
        var user = await factory.NewUserAsync();
        var start = new DateTimeOffset(DateTimeOffset.UtcNow.UtcDateTime.Date.AddDays(3).AddHours(10), TimeSpan.Zero);
        var booking = await (await user.BookAsync(tool.Id, start, start.AddHours(1))).ReadAsync<BookingDto>();

        var deactivate = await admin.PostAsync($"/api/tools/{tool.Id}/deactivate", null);

        Assert.Equal(HttpStatusCode.NoContent, deactivate.StatusCode);
        var mine = await (await user.GetAsync("/api/bookings/mine")).ReadAsync<List<BookingDto>>();
        var cancelled = Assert.Single(mine, b => b.Id == booking.Id);
        Assert.Equal("Cancelled", cancelled.Status);
        Assert.Contains("taken out of service", cancelled.CancellationReason);
    }

    [Fact]
    public async Task Inactive_tools_cannot_be_booked()
    {
        var admin = await factory.AdminAsync();
        var tool = await admin.CreateToolAsync();
        await admin.PostAsync($"/api/tools/{tool.Id}/deactivate", null);
        var user = await factory.NewUserAsync();
        var start = DateTimeOffset.UtcNow.AddDays(1);

        var response = await user.BookAsync(tool.Id, start, start.AddHours(1));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("Bookings.ToolInactive", (await response.ReadAsync<ProblemDto>()).Code);
    }
}

using System.Net;
using ToolBook.IntegrationTests.Infrastructure;

namespace ToolBook.IntegrationTests;

[Collection(ApiCollection.Name)]
public class ConcurrencyTests(ToolBookApiFactory factory)
{
    [Theory]
    [InlineData(false)] // first booking ever for the tool: the ToolSchedule row does not exist yet
    [InlineData(true)]  // the ToolSchedule row already exists
    public async Task Ten_parallel_requests_for_the_same_slot_produce_exactly_one_booking(bool scheduleExists)
    {
        var tool = await (await factory.AdminAsync()).CreateToolAsync();
        var start = new DateTimeOffset(DateTimeOffset.UtcNow.UtcDateTime.Date.AddDays(2).AddHours(9), TimeSpan.Zero);
        if (scheduleExists)
        {
            var warmUp = await factory.NewUserAsync();
            Assert.Equal(HttpStatusCode.Created, (await warmUp.BookAsync(tool.Id, start.AddHours(-3), start.AddHours(-2))).StatusCode);
        }

        var users = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => factory.NewUserAsync()));

        // Release all requests at the same moment.
        using var gate = new SemaphoreSlim(0);
        var requests = users.Select(async user =>
        {
            await gate.WaitAsync();
            return await user.BookAsync(tool.Id, start, start.AddHours(1));
        }).ToList();
        gate.Release(users.Length);
        var responses = await Task.WhenAll(requests);

        var statuses = responses.Select(r => r.StatusCode).ToList();
        Assert.Equal(1, statuses.Count(s => s == HttpStatusCode.Created));
        Assert.Equal(9, statuses.Count(s => s == HttpStatusCode.Conflict));

        // And the database agrees: exactly one active booking in that slot.
        var day = DateOnly.FromDateTime(start.UtcDateTime);
        var view = await (await users[0].GetAsync($"/api/tools/{tool.Id}/bookings?date={day:yyyy-MM-dd}"))
            .ReadAsync<System.Text.Json.JsonElement>();
        Assert.Equal(1, view.GetProperty("bookings").EnumerateArray().Count(b => b.GetProperty("start").GetDateTimeOffset() == start));
    }
}

using System.Net;
using System.Text.Json;
using ToolBook.IntegrationTests.Infrastructure;

namespace ToolBook.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class BookingTests(ToolBookApiFactory factory) : IDisposable
{
    // Tomorrow 08:00 UTC, so bookings are always in the future relative to the real clock.
    private static readonly DateTimeOffset Base = new(DateTimeOffset.UtcNow.UtcDateTime.Date.AddDays(1).AddHours(8), TimeSpan.Zero);

    public void Dispose() => factory.Clock.Reset();

    [Fact]
    public async Task Book_then_list_mine()
    {
        var tool = await (await factory.AdminAsync()).CreateToolAsync();
        var user = await factory.NewUserAsync();

        var created = await user.BookAsync(tool.Id, Base, Base.AddHours(2), "SEM imaging of sample A");

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var booking = await created.ReadAsync<BookingDto>();
        Assert.Equal("Scheduled", booking.Status);
        Assert.Equal(Base, booking.Start);

        var mine = await (await user.GetAsync("/api/bookings/mine")).ReadAsync<List<BookingDto>>();
        Assert.Contains(mine, b => b.Id == booking.Id);
    }

    [Fact]
    public async Task Overlapping_booking_is_rejected_with_conflict()
    {
        var tool = await (await factory.AdminAsync()).CreateToolAsync();
        var alice = await factory.NewUserAsync("Alice");
        var bob = await factory.NewUserAsync("Bob");
        await alice.BookAsync(tool.Id, Base, Base.AddHours(2));

        var response = await bob.BookAsync(tool.Id, Base.AddHours(1), Base.AddHours(3));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("Bookings.Overlap", (await response.ReadAsync<ProblemDto>()).Code);
    }

    [Fact]
    public async Task Cancelling_frees_the_slot()
    {
        var tool = await (await factory.AdminAsync()).CreateToolAsync();
        var alice = await factory.NewUserAsync();
        var bob = await factory.NewUserAsync();
        var booking = await (await alice.BookAsync(tool.Id, Base, Base.AddHours(2))).ReadAsync<BookingDto>();

        var cancel = await alice.PostAsync($"/api/bookings/{booking.Id}/cancel", null);
        var rebook = await bob.BookAsync(tool.Id, Base, Base.AddHours(2));

        Assert.Equal(HttpStatusCode.NoContent, cancel.StatusCode);
        Assert.Equal(HttpStatusCode.Created, rebook.StatusCode);
    }

    [Fact]
    public async Task Only_the_owner_or_an_admin_can_cancel()
    {
        var admin = await factory.AdminAsync();
        var tool = await admin.CreateToolAsync();
        var alice = await factory.NewUserAsync();
        var mallory = await factory.NewUserAsync();
        var booking = await (await alice.BookAsync(tool.Id, Base, Base.AddHours(1))).ReadAsync<BookingDto>();

        var byOther = await mallory.PostAsync($"/api/bookings/{booking.Id}/cancel", null);
        var byAdmin = await admin.PostAsync($"/api/bookings/{booking.Id}/cancel", null);

        Assert.Equal(HttpStatusCode.Forbidden, byOther.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, byAdmin.StatusCode);
    }

    [Fact]
    public async Task Invalid_booking_requests_return_validation_problems()
    {
        var tool = await (await factory.AdminAsync()).CreateToolAsync();
        var user = await factory.NewUserAsync();

        var endBeforeStart = await user.BookAsync(tool.Id, Base.AddHours(2), Base);
        var tooLong = await user.BookAsync(tool.Id, Base, Base.AddHours(9));
        var inThePast = await user.BookAsync(tool.Id, DateTimeOffset.UtcNow.AddHours(-3), DateTimeOffset.UtcNow.AddHours(-2));

        Assert.Equal(HttpStatusCode.BadRequest, endBeforeStart.StatusCode);
        Assert.Contains("End", (await endBeforeStart.ReadAsync<ProblemDto>()).Errors!.Keys);
        Assert.Equal(HttpStatusCode.BadRequest, tooLong.StatusCode);
        Assert.Equal("Bookings.InThePast", (await inThePast.ReadAsync<ProblemDto>()).Code);
    }

    [Fact]
    public async Task Check_in_and_check_out_record_actual_times_and_drive_availability()
    {
        var tool = await (await factory.AdminAsync()).CreateToolAsync();
        var user = await factory.NewUserAsync();
        var booking = await (await user.BookAsync(tool.Id, Base, Base.AddHours(2))).ReadAsync<BookingDto>();

        factory.Clock.Set(Base.AddMinutes(-5));
        var checkIn = await user.PostAsync($"/api/bookings/{booking.Id}/check-in", null);
        Assert.Equal(HttpStatusCode.OK, checkIn.StatusCode);
        Assert.Equal("InUse", (await checkIn.ReadAsync<BookingDto>()).Status);

        factory.Clock.Set(Base.AddMinutes(30));
        var availability = await (await user.GetAsync($"/api/tools/{tool.Id}/availability")).ReadAsync<JsonElement>();
        Assert.Equal("InUse", availability.GetProperty("status").GetString());
        Assert.False(availability.GetProperty("freeNow").GetBoolean());
        Assert.Equal(Base.AddHours(2), availability.GetProperty("estimatedFreeAt").GetDateTimeOffset());
        Assert.Equal("Low", availability.GetProperty("confidence").GetString());

        factory.Clock.Set(Base.AddHours(2).AddMinutes(10));
        var checkOut = await user.PostAsync($"/api/bookings/{booking.Id}/check-out", null);
        var completed = await checkOut.ReadAsync<BookingDto>();
        Assert.Equal("Completed", completed.Status);
        Assert.Equal(Base.AddHours(2).AddMinutes(10), completed.CheckedOutAt);

        var after = await (await user.GetAsync($"/api/tools/{tool.Id}/availability")).ReadAsync<JsonElement>();
        Assert.Equal("Free", after.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Tool_day_view_shows_bookings_with_display_names()
    {
        var tool = await (await factory.AdminAsync()).CreateToolAsync();
        var user = await factory.NewUserAsync("Rosalind Franklin");
        await user.BookAsync(tool.Id, Base, Base.AddHours(1));

        var day = DateOnly.FromDateTime(Base.UtcDateTime);
        var view = await (await user.GetAsync($"/api/tools/{tool.Id}/bookings?date={day:yyyy-MM-dd}")).ReadAsync<JsonElement>();

        var booking = Assert.Single(view.GetProperty("bookings").EnumerateArray());
        Assert.Equal("Rosalind Franklin", booking.GetProperty("userDisplayName").GetString());
    }
}

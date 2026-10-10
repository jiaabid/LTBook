using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ToolBook.Bookings.Domain;
using ToolBook.Bookings.Persistence;
using ToolBook.SharedKernel.Http;
using ToolBook.SharedKernel.Results;
using ToolBook.SharedKernel.Time;
using ToolBook.Tools.Contracts;

namespace ToolBook.Bookings.Features.CreateBooking;

internal sealed record CreateBookingRequest(Guid ToolId, DateTimeOffset Start, DateTimeOffset End, string Purpose);

internal sealed class CreateBookingRequestValidator : AbstractValidator<CreateBookingRequest>
{
    public CreateBookingRequestValidator()
    {
        RuleFor(r => r.ToolId).NotEmpty();
        RuleFor(r => r.Purpose).NotEmpty().MaximumLength(Booking.PurposeMaxLength);
        RuleFor(r => r.End).GreaterThan(r => r.Start).WithMessage("End must be after start.");
        RuleFor(r => r.End - r.Start)
            .InclusiveBetween(Booking.MinDuration, Booking.MaxDuration)
            .When(r => r.End > r.Start)
            .OverridePropertyName("End")
            .WithMessage($"A booking must last between {Booking.MinDuration.TotalMinutes:0} minutes and {Booking.MaxDuration.TotalHours:0} hours.");
    }
}

/// <summary>
/// Creates a booking with two layers of double-booking protection (ADR-0006):
/// 1. the domain overlap check in <see cref="ToolSchedule.Book"/>;
/// 2. the per-tool <see cref="ToolSchedule"/> row's rowversion, which makes concurrent creators for the same tool
///    conflict at commit. The loser retries, re-reads the schedule and is rejected by layer 1.
/// </summary>
internal sealed partial class CreateBookingHandler(
    BookingsDbContext db,
    IToolCatalog tools,
    IClock clock,
    ILogger<CreateBookingHandler> logger)
{
    private const int MaxAttempts = 3;

    public async Task<Result<BookingResponse>> HandleAsync(CreateBookingRequest request, Guid userId, CancellationToken ct)
    {
        var tool = await tools.GetAsync(request.ToolId, ct);
        if (tool is null)
        {
            return BookingErrors.ToolNotFound(request.ToolId);
        }

        if (!tool.IsActive)
        {
            return BookingErrors.ToolInactive(tool.Name);
        }

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            var schedule = await db.ToolSchedules.FirstOrDefaultAsync(s => s.ToolId == tool.Id, ct);
            if (schedule is null)
            {
                schedule = ToolSchedule.For(tool.Id);
                db.ToolSchedules.Add(schedule);
            }

            var candidates = await db.Bookings
                .Where(b => b.ToolId == tool.Id
                            && (b.Status == BookingStatus.Scheduled || b.Status == BookingStatus.InUse)
                            && b.Start < request.End
                            && request.Start < b.End)
                .ToListAsync(ct);

            var result = schedule.Book(userId, request.Start, request.End, request.Purpose, candidates, clock.UtcNow);
            if (!result.IsSuccess)
            {
                return result.Error;
            }

            db.Bookings.Add(result.Value);
            try
            {
                await db.SaveChangesAsync(ct);
                LogBooked(result.Value.Id, tool.Id, attempt);
                return BookingResponse.From(result.Value, tool.Name);
            }
            catch (Exception ex) when (ex is DbUpdateConcurrencyException || ex is DbUpdateException dbEx && dbEx.IsUniqueViolation())
            {
                // Another request changed this tool's schedule (or created it) first. Start over with fresh state.
                LogConcurrentBooking(tool.Id, attempt);
                db.ChangeTracker.Clear();
            }
        }

        return BookingErrors.ConcurrentUpdate;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Booking {BookingId} created for tool {ToolId} (attempt {Attempt})")]
    private partial void LogBooked(Guid bookingId, Guid toolId, int attempt);

    [LoggerMessage(Level = LogLevel.Information, Message = "Concurrent schedule change for tool {ToolId} on attempt {Attempt}; retrying")]
    private partial void LogConcurrentBooking(Guid toolId, int attempt);
}

internal static class CreateBookingEndpoint
{
    public static void MapCreateBooking(this IEndpointRouteBuilder app) =>
        app.MapPost("/", async (CreateBookingRequest request, ClaimsPrincipal user, CreateBookingHandler handler, CancellationToken ct) =>
                (await handler.HandleAsync(request, user.GetUserId(), ct))
                    .ToHttp(booking => TypedResults.Created($"/api/bookings/{booking.Id}", booking)))
            .WithValidation<CreateBookingRequest>()
            .WithName("CreateBooking");
}

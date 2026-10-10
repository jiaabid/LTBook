using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ToolBook.Bookings.Features.CancelBooking;
using ToolBook.Bookings.Features.CancelBookingsOfDeactivatedTool;
using ToolBook.Bookings.Features.CheckIn;
using ToolBook.Bookings.Features.CheckOut;
using ToolBook.Bookings.Features.CreateBooking;
using ToolBook.Bookings.Features.GetAvailability;
using ToolBook.Bookings.Features.ListMyBookings;
using ToolBook.Bookings.Features.ListToolBookings;
using ToolBook.Bookings.Persistence;
using ToolBook.Bookings.Scheduling;
using ToolBook.SharedKernel.Events;
using ToolBook.SharedKernel.Persistence;
using ToolBook.Tools.Contracts;

namespace ToolBook.Bookings;

/// <summary>The only public entry point of the Bookings module: service registration and endpoint mapping.</summary>
public static class BookingsModule
{
    public static IServiceCollection AddBookingsModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<BookingsDbContext>(BookingsDbContext.Schema);
        services.AddSingleton(LabTimeZone.FromConfiguration(configuration));

        services.AddScoped<CreateBookingHandler>();
        services.AddScoped<CancelBookingHandler>();
        services.AddScoped<CheckInHandler>();
        services.AddScoped<CheckOutHandler>();
        services.AddIntegrationEventHandler<ToolDeactivated, ToolDeactivatedHandler>();

        services.AddValidatorsFromAssemblyContaining<BookingsDbContext>(includeInternalTypes: true);
        return services;
    }

    public static IEndpointRouteBuilder MapBookingsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var bookings = endpoints.MapGroup("/bookings").WithTags("Bookings").RequireAuthorization();
        bookings.MapCreateBooking();
        bookings.MapCancelBooking();
        bookings.MapCheckIn();
        bookings.MapCheckOut();
        bookings.MapListMyBookings();

        // Tool-centric reads owned by this module.
        var toolViews = endpoints.MapGroup("/tools").WithTags("Bookings").RequireAuthorization();
        toolViews.MapListToolBookings();
        toolViews.MapGetAvailability();
        return endpoints;
    }
}

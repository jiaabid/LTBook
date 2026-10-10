using ToolBook.SharedKernel.Events;

namespace ToolBook.Tools.Contracts;

/// <summary>Published after a tool has been taken out of service. Bookings cancels the tool's future bookings.</summary>
public sealed record ToolDeactivated(Guid EventId, DateTimeOffset OccurredAt, Guid ToolId, string ToolName) : IIntegrationEvent;

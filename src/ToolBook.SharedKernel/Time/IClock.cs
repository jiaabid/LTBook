namespace ToolBook.SharedKernel.Time;

/// <summary>The current time. Injected everywhere instead of <c>DateTimeOffset.UtcNow</c> so time is testable.</summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

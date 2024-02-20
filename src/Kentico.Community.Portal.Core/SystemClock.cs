namespace Kentico.Community.Portal.Core;

// TODO - replace with TimeProvider and TimeProvider.System

public interface ISystemClock
{
    DateTime Now { get; }
    DateTime UtcNow { get; }
}

public class SystemClock : ISystemClock
{
    public DateTime Now => DateTime.Now;
    public DateTime UtcNow => DateTime.UtcNow;
}

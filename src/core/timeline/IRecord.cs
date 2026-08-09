namespace GrimSpace.Core.Actions;

public interface IRecord : ITimelineEntry;

public sealed record Record<T>(T Value) : IRecord;

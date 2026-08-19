using GrimSpace.Core.Actions;

namespace GrimSpace.Core.Engine;

public sealed record TimelineBatch(int Tick, IReadOnlyList<ITimelineEntry> Entries);

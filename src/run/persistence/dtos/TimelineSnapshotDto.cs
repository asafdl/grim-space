namespace GrimSpace.Run.Persistence;

public sealed record TimelineSnapshotDto(
	int CurrentTick,
	IReadOnlyList<TimelineEntryDto> History,
	IReadOnlyList<TimelineEntryDto> Pending);

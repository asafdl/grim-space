namespace GrimSpace.Run.Persistence;

public sealed record PilotBattleSnapshotDto(
	string PilotId,
	IReadOnlyList<GrimSpace.Pilots.PilotCapabilityGrant> CapabilityGrants,
	string? PortraitId = null);

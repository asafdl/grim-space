using GrimSpace.Battle.Statuses;

namespace GrimSpace.Battle.Effects;

public sealed record ProtectionDroneChangedFacts(
	string SourceId,
	string TargetId,
	BattleStatus? Status);

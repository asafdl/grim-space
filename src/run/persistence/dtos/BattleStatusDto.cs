using GrimSpace.Battle.Statuses;
using GrimSpace.Math.Grid;

namespace GrimSpace.Run.Persistence;

public sealed record BattleStatusDto(
	string InstanceId,
	int Priority,
	IReadOnlyList<string> ActionTags,
	EStatusPolarity Polarity,
	ESpatialOrientation? Face = null,
	FaceShieldDto? Snapshot = null,
	int DamagePending = 0,
	FaceShieldDto? Remaining = null);

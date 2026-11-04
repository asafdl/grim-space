using GrimSpace.Battle.Statuses;
using GrimSpace.Math.Grid;

namespace GrimSpace.Run.Persistence;

public sealed record BattleStatusDto(
	string InstanceId,
	int Priority,
	IReadOnlyList<string> ActionTags,
	EStatusPolarity Polarity,
	ESpatialOrientation? Face,
	FaceShieldDto? Snapshot,
	int DamagePending,
	FaceShieldDto? Remaining);

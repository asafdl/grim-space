using GrimSpace.Battle.Statuses;
using GrimSpace.Math.Grid;

namespace GrimSpace.Run.Persistence;

public sealed record BattleStatusDto(
	string TypeName,
	EStatusPolarity Polarity,
	ESpatialOrientation? Face = null,
	FaceShieldDto? Snapshot = null,
	int DamagePending = 0,
	FaceShieldDto? Remaining = null,
	string? GrantId = null,
	int Stacks = 0);

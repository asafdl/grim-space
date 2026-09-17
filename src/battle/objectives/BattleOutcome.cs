using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Objectives;

public sealed record UnitStateHandoff(
	int HP,
	EType Kind,
	string Id
);

public sealed record BattleOutcome(
	EBattleResult Result,
	IReadOnlyList<UnitStateHandoff> StateHandoffs
);
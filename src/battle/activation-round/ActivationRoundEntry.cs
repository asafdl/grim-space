using GrimSpace.Battle;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.ActivationRound;

public sealed record ActivationRoundEntry(
	EUnitActivationPhase RunAt,
	int? RoundsRemaining,
	bool Sticky,
	IAction Action);

public sealed record UnitActivationRoundPhase(string UnitId, EUnitActivationPhase Phase);

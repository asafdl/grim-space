using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.Units;
using GrimSpace.Units.Loadouts.Defenses;

namespace GrimSpace.Battle.Effects;

public sealed record ShieldsChangedFacts(
	string TargetId,
	FaceShieldPoints ShieldPoints,
	IReadOnlyList<TemporaryShieldGrant> TemporaryShieldGrants,
	BattleStatus? BraceStatus)
{
	public static ShieldsChangedFacts Capture(State state) =>
		new(
			state.Id,
			state.ShieldPoints.Clone(),
			TemporaryShields.ExtractGrants(state),
			ShieldBrace.TryStatus(state)?.DeepCopy());
}

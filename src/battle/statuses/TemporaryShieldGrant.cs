using GrimSpace.Units.Loadouts.Defenses;

namespace GrimSpace.Battle.Statuses;

public sealed class TemporaryShieldGrantStatus : BattleStatus
{
	public override EStatusPolarity Polarity => EStatusPolarity.Buff;
	public required string GrantId { get; init; }
	public required FaceShieldPoints Remaining { get; init; }

	public override BattleStatus DeepCopy() =>
		new TemporaryShieldGrantStatus
		{
			GrantId = GrantId,
			Remaining = Remaining.Clone(),
		};
}

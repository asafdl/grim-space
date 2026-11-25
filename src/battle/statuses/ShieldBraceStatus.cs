using GrimSpace.Math.Grid;
using GrimSpace.Units.Loadouts.Defenses;

namespace GrimSpace.Battle.Statuses;

public sealed class ShieldBraceStatus : BattleStatus
{
	public override EStatusPolarity Polarity => EStatusPolarity.Buff;
	public required ESpatialOrientation Face { get; init; }
	public required FaceShieldPoints Snapshot { get; init; }
	public int DamagePending { get; set; }

	public override BattleStatus DeepCopy() =>
		new ShieldBraceStatus
		{
			Face = Face,
			Snapshot = Snapshot.Clone(),
			DamagePending = DamagePending,
		};
}

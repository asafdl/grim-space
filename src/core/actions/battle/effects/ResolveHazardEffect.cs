using GrimSpace.Battle.Board;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle.Contexts;

namespace GrimSpace.Core.Actions.Battle.Effects;

public sealed class ResolveHazardEffect(string hazardId) : IEffect<BattleSlices>
{
	public void Apply(BattleSlices slices)
	{
		if (!slices.Board.NonUnits.TryGetValue(hazardId, out var nonUnit) || nonUnit is not Hazard hazard)
			return;

		foreach (var unit in slices.Board.Units.Values)
		{
			if (!unit.State.IsAlive || !hazard.Cells.Contains(unit.State.Position))
				continue;

			switch (hazard.Kind)
			{
				case EHazardKind.MissileZone:
					unit.State.Hp = System.Math.Max(unit.State.Hp - hazard.Damage, 0);
					unit.State.MomentumLevel = System.Math.Max(
						unit.State.MomentumLevel - hazard.MomentumLoss,
						0);
					break;
				case EHazardKind.FlakBurst:
					unit.State.MomentumLevel = System.Math.Max(
						unit.State.MomentumLevel - hazard.MomentumLoss,
						0);
					if (unit.State.MomentumLevel < CombatConfig.FlakApPenaltyThreshold)
						unit.State.ApPenaltyNextTurn = true;
					break;
			}
		}
	}
}

using GrimSpace.Battle.Board;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Weapons;
using GrimSpace.Math.Grid;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle.Contexts;

namespace GrimSpace.Core.Actions.Battle.Effects;

public sealed class HazardCellEntryEffect(Coord cell) : IEffect<BattleSlices>
{
	public void Apply(BattleBoard board, State actor)
	{
		foreach (var hazard in board.Hazards)
		{
			if (!hazard.Cells.Contains(cell))
				continue;

			HazardResolution.ApplyToUnitAt(hazard, actor);
		}
	}

	void IEffect<BattleSlices>.Apply(BattleSlices slices) =>
		Apply(slices.Board, slices.Ap.Player);
}

public static class HazardResolution
{
	public static void ApplyToUnitAt(Hazard hazard, State unit)
	{
		switch (hazard.Kind)
		{
			case EHazardKind.MissileZone:
				unit.Hp = System.Math.Max(unit.Hp - hazard.Damage, 0);
				unit.MomentumLevel = System.Math.Max(unit.MomentumLevel - hazard.MomentumLoss, 0);
				break;
			case EHazardKind.FlakBurst:
				unit.MomentumLevel = System.Math.Max(unit.MomentumLevel - hazard.MomentumLoss, 0);
				if (unit.MomentumLevel < CombatConfig.FlakApPenaltyThreshold)
					unit.ApPenaltyNextTurn = true;
				break;
		}
	}
}

using GrimSpace.Battle.Board;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Effects;

public sealed class HazardCellEntryEffect(Coord cell) : IEffect<BattleBoard, ActorSession>
{
	public void Apply(BattleBoard world, ActorSession runtime, string actorId)
	{
		var actor = world.StateOf(actorId);
		foreach (var hazard in world.Hazards)
		{
			if (!hazard.Cells.Contains(cell))
				continue;

			HazardResolution.ApplyToUnitAt(hazard, actor);
		}
	}
}

public static class HazardResolution
{
	public static void ApplyToUnitAt(Hazard hazard, Units.State unit)
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

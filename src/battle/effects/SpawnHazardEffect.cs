using GrimSpace.Battle.Board;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Effects;

public sealed class SpawnHazardEffect(
	string hazardId,
	Coord center,
	EHazardKind kind) : IEffect<BattleBoard, ActorSession>
{
	public void Apply(BattleBoard world, ActorSession runtime, string actorId)
	{
		switch (kind)
		{
			case EHazardKind.MissileZone:
			{
				var ownerFrame = BodyFrame.From(world.StateOf(actorId));
				var hazard = Hazard.MissileZone(
					hazardId,
					actorId,
					center,
					ownerFrame,
					world.Grid,
					CombatConfig.MissileRadius,
					CombatConfig.MissileDamage,
					CombatConfig.MissileMomentumLoss);
				world.MutableNonUnits[hazard.Id] = hazard;
				break;
			}
			default:
				throw new ArgumentOutOfRangeException(nameof(kind));
		}
	}
}

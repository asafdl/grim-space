using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Effects;

public sealed class SpawnTorpedoEffect(ESpatialOrientation mountedOn, string? unitId = null)
	: IEffect<BattleWorld, ActorRuntime>
{
	private Unit? _spawned;

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var units = UnitRegistry.For(world);
		var firer = units.UnitOf(actorId);
		var (position, fore, dorsal) = TorpedoMount.LaunchPose(firer.State, mountedOn);
		var torpedo = Factory.Create(
			new Instance
			{
				Id = unitId ?? string.Empty,
				Type = EType.Torpedo,
				Alliance = firer.Alliance,
			},
			position,
			new TorpedoExecutionAgent(),
			TorpedoConfig.SpawnMomentum,
			fore,
			dorsal);
		torpedo.State.FuelRemaining = TorpedoConfig.Fuel;
		torpedo.State.ParentId = actorId;
		units.Add(torpedo);
		_spawned = torpedo;

		return
		[
			new Record<SpawnFacts>(new SpawnFacts(
				SourceId: actorId,
				TargetId: torpedo.State.Id,
				EntityType: EType.Torpedo)),
		];
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		if (_spawned is null)
			return;

		UnitRegistry.For(world).Remove(_spawned.State.Id);
		_spawned = null;
	}
}

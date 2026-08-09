using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Weapons;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Effects;

public sealed class SpawnTorpedoEffect(ETorpedoMount mount, string? unitId = null)
	: IEffect<BattleWorld, ActorRuntime>
{
	private Unit? _spawned;

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var units = UnitRegistry.For(world);
		var firer = units.UnitOf(actorId);
		var (position, fore, dorsal) = TorpedoMount.LaunchPose(firer.State, mount);
		var torpedo = Factory.Create(
			new Instance
			{
				Id = unitId ?? string.Empty,
				Type = EType.Torpedo,
				Alliance = firer.Alliance,
			},
			position,
			world.IdRegistry,
			TorpedoConfig.SpawnMomentum,
			fore,
			dorsal);
		torpedo.State.FuelRemaining = TorpedoConfig.Fuel;
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

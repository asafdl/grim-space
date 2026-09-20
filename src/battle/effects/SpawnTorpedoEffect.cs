using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Battle.Effects;

public sealed class SpawnTorpedoEffect(AbilityMount mount, string unitId)
	: IEffect<BattleWorld, ActorRuntime>
{
	private Unit? _spawned;

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var units = UnitRegistry.For(world);
		var firer = units.UnitOf(actorId);
		var (position, fore, dorsal) = TorpedoMount.LaunchPose(firer.State, mount.Facet);
		var child = Factory.ChildFromSpawnableMount(firer.State, mount, unitId);
		var torpedo = Factory.Create(
			child,
			firer.Team,
			position,
			new TorpedoExecutionAgent(),
			fore,
			dorsal);
		torpedo.State.FuelRemaining = TorpedoBodySpec.Require(torpedo.State.Spec).FuelTurns;
		torpedo.State.ParentId = actorId;
		units.Add(torpedo);
		_spawned = torpedo;

		return
		[
			new Record<SpawnFacts>(new SpawnFacts(
				SourceId: actorId,
				TargetId: torpedo.State.Id,
				EntityType: EType.Torpedo,
				SpawnedState: torpedo.State.Clone())),
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

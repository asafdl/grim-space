using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Battle.Effects;

public sealed class SpawnPatrolEffect(string unitId) : IEffect<BattleWorld, ActorRuntime>
{
	private Unit? _spawned;

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var units = UnitRegistry.For(world);
		var parent = units.UnitOf(actorId);
		var (position, fore, dorsal) = PatrolBayMount.LaunchPose(parent.State);
		var child = Factory.ChildFromSpawnableMount(parent.State, EAbilityKind.PatrolBay, unitId);
		var patrol = Factory.Create(
			child,
			parent.Team,
			position,
			new AiController(),
			fore,
			dorsal,
			parentId: actorId);
		units.Add(patrol);
		_spawned = patrol;

		return
		[
			new Record<SpawnFacts>(new SpawnFacts(
				SourceId: actorId,
				TargetId: patrol.State.Id,
				EntityType: EType.Patrol,
				SpawnedState: patrol.State.Clone())),
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

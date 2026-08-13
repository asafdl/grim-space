using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Effects;

public sealed class SpawnPatrolEffect(string? unitId = null) : IEffect<BattleWorld, ActorRuntime>
{
	private Unit? _spawned;

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var units = UnitRegistry.For(world);
		var parent = units.UnitOf(actorId);
		var (position, fore, dorsal) = PatrolBayMount.LaunchPose(parent.State);
		var patrol = Factory.Create(
			new Instance
			{
				Id = unitId ?? string.Empty,
				Type = EType.Patrol,
				Alliance = parent.Alliance,
			},
			position,
			new AiController(),
			world.IdRegistry,
			PatrolBayMount.SpawnMomentum(parent.State),
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
				EntityType: EType.Patrol)),
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

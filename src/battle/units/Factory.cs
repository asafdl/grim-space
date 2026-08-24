using GrimSpace.Battle.Ids;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Engine;
using GrimSpace.Core.Ids;
using GrimSpace.Math.Grid;
using GrimSpace.Units;

namespace GrimSpace.Battle.Units;

public static class Factory
{
	public static Unit Create(
		Instance instance,
		Coord position,
		ExecutionAgent<BattleWorld, ActorRuntime> executionAgent,
		int initialMomentum = 0) =>
		Create(instance, position, executionAgent, initialMomentum, Coord.Forward, Coord.Up);

	public static Unit Create(
		Instance instance,
		Coord position,
		ExecutionAgent<BattleWorld, ActorRuntime> executionAgent,
		int initialMomentum,
		Coord fore,
		Coord dorsal,
		string parentId = BattleActorIds.Rules)
	{
		var id = ResolveId(instance);
		var state = State.FromSpawn(new Instance
		{
			Id = id,
			Type = instance.Type,
			Alliance = instance.Alliance,
		}, position, fore, dorsal, parentId);
		state.MomentumLevel = System.Math.Clamp(initialMomentum, 0, MomentumConfig.MaxLevel);
		return new Unit(instance.Alliance, state, executionAgent);
	}

	private static string ResolveId(Instance instance) =>
		!string.IsNullOrWhiteSpace(instance.Id)
			? instance.Id
			: TypedIdGenerator.NextId(UnitTypeSlug.For(instance.Type));
}

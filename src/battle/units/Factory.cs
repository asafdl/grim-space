using GrimSpace.Battle.Ids;
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
		ExecutionAgent<BattleWorld, ActorRuntime> executionAgent) =>
		Create(instance, position, executionAgent, Coord.Forward, Coord.Up);

	public static Unit Create(
		Instance instance,
		Coord position,
		ExecutionAgent<BattleWorld, ActorRuntime> executionAgent,
		Coord fore,
		Coord dorsal,
		string parentId = BattleActorIds.Rules)
	{
		var id = ResolveId(instance);
		var state = State.FromSpawn(new Instance
		{
			Id = id,
			Type = instance.Type,
			Team = instance.Team,
		}, position, fore, dorsal, parentId);
		return new Unit(state, executionAgent, instance.Team);
	}

	private static string ResolveId(Instance instance) =>
		!string.IsNullOrWhiteSpace(instance.Id)
			? instance.Id
			: TypedIdGenerator.NextId(UnitTypeSlug.For(instance.Type));
}

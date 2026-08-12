using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Ids;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Units;

public static class Factory
{
	public static Unit Create(
		Instance instance,
		Coord position,
		UnitIdRegistry? ids = null,
		int initialMomentum = 0) =>
		Create(instance, position, ids, initialMomentum, Coord.Forward, Coord.Up);

	public static Unit Create(
		Instance instance,
		Coord position,
		UnitIdRegistry? ids,
		int initialMomentum,
		Coord fore,
		Coord dorsal)
	{
		var id = ResolveId(instance, ids);
		var state = State.FromSpawn(new Instance
		{
			Id = id,
			Type = instance.Type,
			Alliance = instance.Alliance,
		}, position, fore, dorsal);
		state.MomentumLevel = System.Math.Clamp(initialMomentum, 0, MomentumConfig.MaxLevel);
		return new Unit(instance.Alliance, state, ExecutionAgentFor(instance));
	}

	private static string ResolveId(Instance instance, UnitIdRegistry? ids)
	{
		if (!string.IsNullOrWhiteSpace(instance.Id))
		{
			ids?.Register(instance.Id);
			return instance.Id;
		}

		if (ids is null)
			throw new InvalidOperationException("Unit id is required when no UnitIdRegistry is provided.");

		return ids.NextUnitId(instance.Type);
	}

	private static IExecutionAgent<BattleWorld, ActorRuntime, Unit> ExecutionAgentFor(Instance instance) =>
		instance.Type == EType.Torpedo ? TorpedoExecutionAgent.Instance
		: instance.Alliance.Team == ETeam.Player ? new HumanExecutionAgent()
		: AiController.Instance;
}

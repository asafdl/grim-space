using GrimSpace.Battle.Ids;
using GrimSpace.Battle.Movement;
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
		int initialMomentum = 0)
	{
		var id = ResolveId(instance, ids);
		var state = State.FromSpawn(new Instance
		{
			Id = id,
			Type = instance.Type,
			Controller = instance.Controller,
		}, position);
		state.MomentumLevel = System.Math.Clamp(initialMomentum, 0, MomentumConfig.MaxLevel);
		var movement = MovementFor(instance.Type);
		return ShellFor(instance.Controller, state, movement);
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

	private static IMovement MovementFor(EType type) =>
		type switch
		{
			EType.Fighter => new DiscreteStep(),
			_ => throw new ArgumentOutOfRangeException(nameof(type)),
		};

	private static Unit ShellFor(EController controller, State state, IMovement movement) =>
		controller switch
		{
			EController.Player => new Player(state, movement),
			EController.Enemy => new EnemyUnit(state, movement),
			_ => throw new ArgumentOutOfRangeException(nameof(controller)),
		};
}

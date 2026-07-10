using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Grid;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Units.Enums;

namespace GrimSpace.Battle.Units;

public static class Factory
{
	public static Unit Create(Blueprint blueprint)
	{
		var stats = StatsFor(blueprint.Type);
		var state = State.CreateDefault(blueprint.Id, blueprint.Position, stats);
		var movement = MovementFor(blueprint.Type);
		var actions = ActionsFor(blueprint.Type);
		return ShellFor(blueprint.Controller, state, movement, actions);
	}

	private static Stats StatsFor(EType type) =>
		type switch
		{
			EType.Fighter => new Stats
			{
				MaxAp = 4,
				MainThrustPower = 2,
				RetroThrustPower = 1,
				LateralThrustPower = 1,
			},
			_ => throw new ArgumentOutOfRangeException(nameof(type)),
		};

	private static IMovement MovementFor(EType type) =>
		type switch
		{
			EType.Fighter => new DiscreteStep(),
			_ => throw new ArgumentOutOfRangeException(nameof(type)),
		};

	private static IActions ActionsFor(EType type) =>
		type switch
		{
			EType.Fighter => new Fighter(),
			_ => throw new ArgumentOutOfRangeException(nameof(type)),
		};

	private static Unit ShellFor(EController controller, State state, IMovement movement, IActions actions) =>
		controller switch
		{
			EController.Player => new Player(state, movement, actions),
			EController.Enemy => new Enemy(state, movement, actions),
			_ => throw new ArgumentOutOfRangeException(nameof(controller)),
		};
}

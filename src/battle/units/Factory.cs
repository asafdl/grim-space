using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Movement;
using GrimSpace.Domain.Grid;
using GrimSpace.Domain.Units;
using GrimSpace.Domain.Units.Enums;

namespace GrimSpace.Battle.Units;

public static class Factory
{
	public static Unit Create(Instance instance, Coord position)
	{
		var state = State.FromSpawn(instance, position);
		var movement = MovementFor(instance.Type);
		var actions = ActionsFor(instance.Type);
		return ShellFor(instance.Controller, state, movement, actions);
	}

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

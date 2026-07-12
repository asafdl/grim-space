using GrimSpace.Math.Grid;
using GrimSpace.Battle.Movement;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Units;

public static class Factory
{
	public static Unit Create(Instance instance, Coord position, int initialMomentum = 0)
	{
		var state = State.FromSpawn(instance, position);
		state.MomentumLevel = System.Math.Clamp(initialMomentum, 0, MomentumConfig.MaxLevel);
		var movement = MovementFor(instance.Type);
		return ShellFor(instance.Controller, state, movement);
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

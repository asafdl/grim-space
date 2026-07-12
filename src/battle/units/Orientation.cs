using GrimSpace.Battle.Units;
using GrimSpace.Domain.Units;
using GrimSpace.Domain.Units.Enums;

namespace GrimSpace.Battle.Units;

public static class Orientation
{
	public static void ApplyRoll(State state, ERollDirection direction)
	{
		var basis = ShipOrientation.Roll(CurrentBasis(state), direction);
		ApplyBasis(state, basis);
	}

	public static void ApplyHeadingTurn(State state, EHeadingTurn turn)
	{
		var basis = ShipOrientation.HeadingTurn(CurrentBasis(state), turn);
		ApplyBasis(state, basis);
		state.MomentumLevel = Math.Max(state.MomentumLevel - 1, 0);
	}

	private static ShipBasis CurrentBasis(State state) =>
		ShipBasis.From(state.ForwardDirection, state.UpDirection, state.RightDirection);

	private static void ApplyBasis(State state, ShipBasis basis)
	{
		state.ForwardDirection = basis.Forward;
		state.UpDirection = basis.Up;
		state.RightDirection = basis.Right;
	}
}

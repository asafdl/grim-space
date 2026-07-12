using GrimSpace.Battle.Units;

namespace GrimSpace.Battle.Planning;

internal static class StateSnapshot
{
	public static State Clone(State source) =>
		new()
		{
			Id = source.Id,
			Position = source.Position,
			ForwardDirection = source.ForwardDirection,
			UpDirection = source.UpDirection,
			RightDirection = source.RightDirection,
			ActionPoints = source.ActionPoints,
			Hp = source.Hp,
			MomentumLevel = source.MomentumLevel,
			Stats = source.Stats,
		};
}

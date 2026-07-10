using GrimSpace.Battle.Actions.Enums;
using GrimSpace.Battle.Grid;
using GrimSpace.Battle.Units;
using BattleGrid = GrimSpace.Battle.Grid.Grid;

namespace GrimSpace.Battle.Movement;

public sealed class DiscreteStep : IMovement
{
	public const int StepCount = 4;

	public IReadOnlyList<Option> GetPreviews(State unit, BattleGrid grid)
	{
		var options = new List<Option>
		{
			BuildOption(unit, lateral: null),
			BuildOption(unit, ELateralDirection.Dorsal),
			BuildOption(unit, ELateralDirection.Ventral),
			BuildOption(unit, ELateralDirection.Port),
			BuildOption(unit, ELateralDirection.Starboard),
		};

		options.RemoveAll(o => !IsPathInBounds(grid, o.Path));
		return options;
	}

	public bool CanMove(State unit, Option option) =>
		option.Path.Count == StepCount;

	public void ApplyMove(State unit, Option option) =>
		unit.Position = option.EndPosition;

	private static Option BuildOption(State unit, ELateralDirection? lateral)
	{
		var forwardSteps = lateral is null ? StepCount : StepCount - 1;
		var path = new List<Coord>(StepCount);
		var pos = unit.Position;

		for (var i = 0; i < forwardSteps; i++)
		{
			pos += unit.ForwardDirection;
			path.Add(pos);
		}

		if (lateral is not null)
		{
			pos += LateralDelta(unit, lateral.Value);
			path.Add(pos);
		}

		return new Option
		{
			Lateral = lateral,
			Path = path,
		};
	}

	private static Coord LateralDelta(State unit, ELateralDirection direction) =>
		direction switch
		{
			ELateralDirection.Dorsal => unit.UpDirection,
			ELateralDirection.Ventral => Coord.Zero - unit.UpDirection,
			ELateralDirection.Starboard => unit.RightDirection,
			ELateralDirection.Port => Coord.Zero - unit.RightDirection,
			_ => Coord.Zero,
		};

	private static bool IsPathInBounds(BattleGrid grid, IReadOnlyList<Coord> path)
	{
		foreach (var cell in path)
		{
			if (!grid.IsInBounds(cell))
				return false;
		}

		return true;
	}
}

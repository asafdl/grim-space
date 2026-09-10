using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Dfs;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Movement;

public static class MovePathEndpoints
{
	public static IReadOnlyList<MovePathSession> DiscoverExtensions(
		Simulation<BattleWorld, ActorRuntime> sim,
		string actorId)
	{
		var start = sim.StateOf<ActorState>(actorId);
		var startDepth = sim.Actions.Count;
		var startCheckpoint = new MoveCheckpoint(
			start.Position,
			GridBasis.From(start.Fore, start.Dorsal, start.Starboard));
		var results = new Dictionary<(Coord Position, GridBasis Basis), MovePathSession>();

		foreach (var frame in ActionSearch.Run(
			sim,
			actorId,
			[MoveDef.Instance],
			BattleSearchVisit.ForMovePreview))
		{
			if (frame.Depth <= 0)
				continue;

			var actor = frame.World.StateOf(actorId);
			if (!actor.IsAlive)
				continue;

			var steps = frame.Actions
				.Skip(startDepth)
				.Cast<MoveStepAction>()
				.ToList();
			var checkpoints = BuildCheckpoints(startCheckpoint, steps);
			var session = new MovePathSession(
				actorId,
				steps,
				checkpoints,
				actor.ActionPoints,
				actor.Clone());
			var key = (session.EndPosition, session.EndBasis);

			if (!results.TryGetValue(key, out var existing) || Compare(session, existing) < 0)
				results[key] = session;
		}

		return results.Values
			.OrderBy(path => path.EndPosition.X)
			.ThenBy(path => path.EndPosition.Y)
			.ThenBy(path => path.EndPosition.Z)
			.ThenBy(path => path, MovePathRankComparer.Instance)
			.ThenBy(path => path.EndBasis.Forward.X)
			.ThenBy(path => path.EndBasis.Forward.Y)
			.ThenBy(path => path.EndBasis.Forward.Z)
			.ThenBy(path => path.EndBasis.Up.X)
			.ThenBy(path => path.EndBasis.Up.Y)
			.ThenBy(path => path.EndBasis.Up.Z)
			.ToList();
	}

	private static IReadOnlyList<MoveCheckpoint> BuildCheckpoints(
		MoveCheckpoint start,
		IReadOnlyList<MoveStepAction> steps)
	{
		var checkpoints = new List<MoveCheckpoint>(steps.Count + 1) { start };
		var position = start.Position;
		var basis = start.Basis;

		foreach (var step in steps)
		{
			var headingBasis = step.Heading is { } heading
				? Orientation.HeadingTurn(basis, heading)
				: basis;
			position += headingBasis.Forward;
			basis = step.Roll is { } roll
				? Orientation.Roll(headingBasis, roll)
				: headingBasis;
			checkpoints.Add(new MoveCheckpoint(position, basis));
		}

		return checkpoints;
	}

	private static int Compare(MovePathSession left, MovePathSession right) =>
		MovePathRankComparer.Instance.Compare(left, right);

	private sealed class MovePathRankComparer : IComparer<MovePathSession>
	{
		public static MovePathRankComparer Instance { get; } = new();

		public int Compare(MovePathSession? left, MovePathSession? right)
		{
			if (ReferenceEquals(left, right))
				return 0;
			if (left is null)
				return 1;
			if (right is null)
				return -1;

			var comparison = left.Steps.Count.CompareTo(right.Steps.Count);
			if (comparison != 0)
				return comparison;

			comparison = left.Steps.Count(step => step.Heading is not null)
				.CompareTo(right.Steps.Count(step => step.Heading is not null));
			if (comparison != 0)
				return comparison;

			comparison = left.Steps.Count(step => step.Roll is not null)
				.CompareTo(right.Steps.Count(step => step.Roll is not null));
			if (comparison != 0)
				return comparison;

			for (var i = 0; i < left.Steps.Count; i++)
			{
				comparison = HeadingOrder(left.Steps[i].Heading).CompareTo(HeadingOrder(right.Steps[i].Heading));
				if (comparison != 0)
					return comparison;

				comparison = RollOrder(left.Steps[i].Roll).CompareTo(RollOrder(right.Steps[i].Roll));
				if (comparison != 0)
					return comparison;
			}

			return 0;
		}

		private static int HeadingOrder(Movement.Enums.EHeadingTurn? heading) =>
			heading switch
			{
				null => 0,
				Movement.Enums.EHeadingTurn.YawLeft => 1,
				Movement.Enums.EHeadingTurn.YawRight => 2,
				Movement.Enums.EHeadingTurn.PitchUp => 3,
				Movement.Enums.EHeadingTurn.PitchDown => 4,
				_ => int.MaxValue,
			};

		private static int RollOrder(Movement.Enums.ERollDirection? roll) =>
			roll switch
			{
				null => 0,
				Movement.Enums.ERollDirection.Clockwise => 1,
				Movement.Enums.ERollDirection.CounterClockwise => 2,
				_ => int.MaxValue,
			};
	}
}

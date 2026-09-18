using GrimSpace.Battle;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tutorials;

public sealed record BattleTutorialObjective(
	Coord Destination,
	GridBasis RequiredBasis);

public abstract record BattleTutorialObjectiveResult
{
	public sealed record Resolved(BattleTutorialObjective Objective) : BattleTutorialObjectiveResult;

	public sealed record Unreachable(string Reason) : BattleTutorialObjectiveResult;
}

public static class BattleTutorialObjectives
{
	public static BattleTutorialObjectiveResult ResolveTurn1(
		BattleOrchestrator battle,
		GridBasis initialBasis)
	{
		var player = PlayerState(battle);
		var destination = initialBasis.ToWorldCell(player.Position, 4, 0, 0);
		return ResolveMoveObjective(
			battle,
			destination,
			initialBasis,
			"Turn 1 objective is unreachable.");
	}

	public static BattleTutorialObjectiveResult ResolveTurn2(BattleOrchestrator battle)
	{
		var player = PlayerState(battle);
		var basis = GridBasis.From(player.Fore, player.Dorsal, player.Starboard);
		var destination = basis.ToWorldCell(player.Position, 2, 0, 2);
		var enemyDir = NearestEnemyDirection(battle, player.Position);
		if (enemyDir is null)
			return new BattleTutorialObjectiveResult.Unreachable("No living enemies to orient toward.");

		var pitched = Orientation.HeadingTurn(basis, EHeadingTurn.PitchUp);
		var rollCandidates = new[]
		{
			pitched,
			Orientation.Roll(pitched, ERollDirection.Clockwise),
			Orientation.Roll(pitched, ERollDirection.CounterClockwise),
		};

		foreach (var candidate in rollCandidates)
		{
			if (VentralDirection(candidate) != enemyDir)
				continue;

			return new BattleTutorialObjectiveResult.Resolved(
				new BattleTutorialObjective(destination, candidate));
		}

		return new BattleTutorialObjectiveResult.Unreachable(
			"No pose places ventral toward the nearest enemy.");
	}

	public static bool IsReachable(
		BattleOrchestrator battle,
		BattleTutorialObjective objective) =>
		MovePathEndpoints.DiscoverExtensions(battle.PlayerAgent.Sim, battle.PlayerId)
			.Any(session =>
				session.EndPosition == objective.Destination
				&& session.EndBasis == objective.RequiredBasis
				&& session.Steps.Count > 0);

	private static BattleTutorialObjectiveResult ResolveMoveObjective(
		BattleOrchestrator battle,
		Coord destination,
		GridBasis requiredBasis,
		string failureReason)
	{
		var sessions = MovePathEndpoints.DiscoverExtensions(battle.PlayerAgent.Sim, battle.PlayerId);
		var legal = sessions.Any(session =>
			session.EndPosition == destination
			&& session.EndBasis == requiredBasis
			&& session.Steps.Count > 0);
		return legal
			? new BattleTutorialObjectiveResult.Resolved(new BattleTutorialObjective(destination, requiredBasis))
			: new BattleTutorialObjectiveResult.Unreachable(failureReason);
	}

	private static State PlayerState(BattleOrchestrator battle) =>
		battle.Engine.World.StateOf(battle.PlayerId);

	private static Coord? NearestEnemyDirection(BattleOrchestrator battle, Coord origin)
	{
		Coord? bestDirection = null;
		var bestDistance = int.MaxValue;
		foreach (var unit in UnitRegistry.For(battle.Engine.World).All)
		{
			if (unit.Team != ETeam.Enemy || !unit.State.IsAlive)
				continue;

			var delta = unit.State.Position - origin;
			if (!TryDominantAxisDirection(delta, out var direction))
				continue;

			var distance = System.Math.Abs(delta.X) + System.Math.Abs(delta.Y) + System.Math.Abs(delta.Z);
			if (distance >= bestDistance)
				continue;

			bestDistance = distance;
			bestDirection = direction;
		}

		return bestDirection;
	}

	private static bool TryDominantAxisDirection(Coord delta, out Coord direction)
	{
		direction = default;
		if (delta == Coord.Zero)
			return false;

		var absX = System.Math.Abs(delta.X);
		var absY = System.Math.Abs(delta.Y);
		var absZ = System.Math.Abs(delta.Z);
		if (absX >= absY && absX >= absZ)
			direction = new Coord(System.Math.Sign(delta.X), 0, 0);
		else if (absY >= absX && absY >= absZ)
			direction = new Coord(0, System.Math.Sign(delta.Y), 0);
		else
			direction = new Coord(0, 0, System.Math.Sign(delta.Z));
		return true;
	}

	private static Coord VentralDirection(GridBasis basis) => Coord.Zero - basis.Up;
}

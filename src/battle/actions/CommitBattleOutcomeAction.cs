using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Ids;
using GrimSpace.Battle.Objectives;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Actions;

public enum EBattleOutcomeCommit
{
	Evaluate,
	Retire,
	Force,
}

public sealed record CommitBattleOutcomeAction(
	EBattleOutcomeCommit Commit,
	string? PlayerId = null,
	EBattleResult? ForcedResult = null) : IAction<BattleWorld, ActorRuntime>
{
	public string ActorId => BattleActorIds.Rules;

	public IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Definition =>
		CommitBattleOutcomeDef.Instance;
}

public sealed class CommitBattleOutcomeDef
	: IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>
{
	public static CommitBattleOutcomeDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(BattleWorld world, ActorRuntime runtime, string actorId) => [];

	public CommitBattleOutcomeAction BindEvaluate() =>
		new(EBattleOutcomeCommit.Evaluate);

	public CommitBattleOutcomeAction BindRetire() =>
		new(EBattleOutcomeCommit.Retire);

	public CommitBattleOutcomeAction BindForce(EBattleResult forcedResult, string playerId) =>
		new(EBattleOutcomeCommit.Force, playerId, forcedResult);

	public bool IsPossible(IAction action, BattleWorld world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, BattleWorld world, ActorRuntime runtime)
	{
		if (world.battleResult != EBattleResult.Ongoing)
			return false;

		if (action is not CommitBattleOutcomeAction commit)
			return false;

		return commit.Commit switch
		{
			EBattleOutcomeCommit.Evaluate or EBattleOutcomeCommit.Retire => true,
			EBattleOutcomeCommit.Force =>
				commit.ForcedResult is EBattleResult.Win or EBattleResult.Lose
				&& !string.IsNullOrEmpty(commit.PlayerId),
			_ => false,
		};
	}

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		IAction action,
		BattleWorld world,
		ActorRuntime runtime)
	{
		var commit = (CommitBattleOutcomeAction)action;
		return
		[
			new CommitBattleOutcomeEffect(commit.Commit, commit.PlayerId, commit.ForcedResult),
		];
	}
}

public static class CommitBattleOutcomeRules
{
	public static EBattleResult EvaluateObjective(BattleWorld world, ETeam team) =>
		world.Objective switch
		{
			EObjective.EliminateOpponents => EvaluateEliminateOpponents(UnitRegistry.For(world), team),
			_ => throw new ArgumentOutOfRangeException(nameof(world.Objective), world.Objective, null),
		};

	public static BattleOutcome CaptureOutcome(BattleWorld world, EBattleResult result) =>
		new(
			world.BattleId,
			result,
			UnitRegistry.For(world).All
				.Where(unit => world.EngagedShipIds.Contains(unit.State.Id))
				.Select(unit => new UnitStateHandoff(
					unit.State.Id,
					unit.State.Type,
					unit.State.HullPoints,
					unit.State.ShieldPoints.Clone()))
				.ToArray());

	private static EBattleResult EvaluateEliminateOpponents(UnitRegistry units, ETeam team)
	{
		var teamAlive = units.All
			.GroupBy(unit => unit.Team)
			.ToDictionary(
				group => group.Key,
				group => group.Any(unit => unit.State.IsAlive));

		var perspectiveAlive = teamAlive.GetValueOrDefault(team);
		var anyOtherAlive = teamAlive
			.Where(pair => pair.Key != team)
			.Any(pair => pair.Value);
		return (perspectiveAlive, anyOtherAlive) switch
		{
			(true, false) => EBattleResult.Win,
			(false, true) => EBattleResult.Lose,
			(false, false) => EBattleResult.Tie,
			_ => EBattleResult.Ongoing,
		};
	}
}

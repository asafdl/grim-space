using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Objectives;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Battle.Runtime;
using GrimSpace.Core.Actions;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Effects;

public sealed class CommitBattleOutcomeEffect(
	EBattleOutcomeCommit commit,
	string? playerId,
	EBattleResult? forcedResult) : IEffect<BattleWorld, ActorRuntime>
{
	private EBattleResult _previousBattleResult;
	private Dictionary<string, int>? _previousHullByUnitId;

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		_previousBattleResult = world.battleResult;
		var units = UnitRegistry.For(world);

		var result = commit switch
		{
			EBattleOutcomeCommit.Evaluate => CommitBattleOutcomeRules.EvaluateObjective(world, ETeam.Player),
			EBattleOutcomeCommit.Retire => EBattleResult.Lose,
			EBattleOutcomeCommit.Force => ApplyForce(units, world),
			_ => throw new ArgumentOutOfRangeException(nameof(commit), commit, null),
		};

		world.battleResult = result;
		return BattleOutcomeRecords.ForResult(world, result);
	}

	private EBattleResult ApplyForce(UnitRegistry units, BattleWorld world)
	{
		if (forcedResult is not (EBattleResult.Win or EBattleResult.Lose))
			throw new InvalidOperationException("Force commit requires win or lose.");
		if (string.IsNullOrEmpty(playerId))
			throw new InvalidOperationException("Force commit requires a player id.");

		var player = units.UnitOf(playerId);
		var targets = forcedResult == EBattleResult.Win
			? units.All.Where(unit => player.RelationTo(unit) == EUnitRelation.Opponent)
			: [player];

		_previousHullByUnitId = new Dictionary<string, int>(StringComparer.Ordinal);
		foreach (var target in targets)
		{
			_previousHullByUnitId[target.State.Id] = target.State.HullPoints;
			target.State.HullPoints = 0;
		}

		return CommitBattleOutcomeRules.EvaluateObjective(world, ETeam.Player);
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		world.battleResult = _previousBattleResult;
		if (_previousHullByUnitId is null)
			return;

		foreach (var (unitId, hull) in _previousHullByUnitId)
			world.StateOf(unitId).HullPoints = hull;
	}
}

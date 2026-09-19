using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Objectives;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Log;
using GrimSpace.Tutorials;
using GrimSpace.World.StarSystem;

namespace GrimSpace.Run;

public sealed class State
{
	//TODO: player fleet should not be hardcoded here
	public const string PlayerFleetUnitId = "player-fleet";

	public Party PlayerParty { get; } = new();
	public TutorialProgress TutorialProgress { get; } = new();
	public StarSystemOrchestrator StarSystem { get; set; } = null!;
	public BattleEncounter? ActiveBattle { get; internal set; }
	public BattleOutcome? PendingBattleOutcome { get; private set; }

	private readonly HashSet<string> _resolvedBattleIds = new(StringComparer.Ordinal);

	public void OnCommittedBattleOutcome(Record<BattleOutcome> record)
	{
		var outcome = record.Value;
		if (outcome.Result == EBattleResult.Ongoing)
			return;

		if (ActiveBattle is null)
			return;

		if (!string.Equals(outcome.BattleId, ActiveBattle.Id, StringComparison.Ordinal))
		{
			GameLog.Log(
				$"Ignoring battle outcome '{outcome.BattleId}'; active engagement is '{ActiveBattle.Id}'.");
			return;
		}

		if (_resolvedBattleIds.Contains(outcome.BattleId))
			return;

		if (!StarSystem.ResolveEngagement(PlayerFleetUnitId, outcome))
		{
			PendingBattleOutcome = outcome;
			GameLog.Log("Engagement resolution failed; outcome retained.");
			return;
		}

		_resolvedBattleIds.Add(outcome.BattleId);
		PendingBattleOutcome = null;
		ActiveBattle = null;
	}

	public static State CreateNewRun(int seed = 0)
	{
		var run = new State();
		run.StarSystem = StarSystemOrchestrator.CreateSession(PlayerFleetUnitId, seed);
		var playerFleet = run.StarSystem.Map.FleetRegistry.FleetOf(PlayerFleetUnitId);
		foreach (var member in playerFleet.Members)
			run.PlayerParty.Add(member);
		return run;
	}
}

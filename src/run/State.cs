using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Objectives;
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

	public bool TryResolveActiveBattle(BattleOutcome outcome)
	{
		if (ActiveBattle is null)
		{
			return false;
		}

		if (!StarSystem.ResolveEngagement(PlayerFleetUnitId, outcome))
			return false;

		ActiveBattle = null;
		return true;
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

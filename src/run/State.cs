using GrimSpace.Battle.Objectives;
using GrimSpace.Tutorials;
using GrimSpace.World.StarSystem;

namespace GrimSpace.Run;

public sealed class State
{
	public const string PlayerFleetUnitId = "player-fleet";

	public Party PlayerParty { get; } = new();
	public TutorialProgress TutorialProgress { get; } = new();
	public StarSystemOrchestrator StarSystem { get; set; } = null!;
	public ActiveBattle? ActiveBattle { get; internal set; }

	public bool TryResolveActiveBattle(BattleOutcome outcome)
	{
		if (ActiveBattle is null
			|| !outcome.IsOver
			|| !ActiveBattle.ParticipantUnitIds
				.ToHashSet(StringComparer.Ordinal)
				.SetEquals(outcome.ParticipantStates.Keys))
		{
			return false;
		}

		if (!StarSystem.ResolveEngagement(PlayerFleetUnitId, outcome))
			return false;

		ActiveBattle = null;
		return true;
	}

	public static State CreateDevDefault(int seed = 0)
	{
		var run = new State();
		run.StarSystem = StarSystemOrchestrator.CreateDevSession(PlayerFleetUnitId, seed);
		var playerFleet = run.StarSystem.Map.FleetRegistry.FleetOf(PlayerFleetUnitId);
		foreach (var member in playerFleet.Members)
			run.PlayerParty.Add(member);
		return run;
	}
}

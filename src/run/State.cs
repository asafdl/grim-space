using GrimSpace.Units;
using GrimSpace.Units.Enums;
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

	public static State CreateDevDefault(int seed = 0)
	{
		var run = new State();
		run.PlayerParty.Add(FleetMember.Create(EType.Fighter));
		run.StarSystem = StarSystemOrchestrator.CreateDevSession(
			PlayerFleetUnitId,
			run.PlayerParty.Members,
			seed);
		return run;
	}
}

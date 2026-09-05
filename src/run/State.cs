using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem;

namespace GrimSpace.Run;

public sealed class State
{
	public const string PlayerFleetUnitId = "player-fleet";

	public Party PlayerParty { get; } = new();
	public StarSystemOrchestrator StarSystem { get; set; } = null!;

	public static State CreateDevDefault(int seed = 0)
	{
		var run = new State();
		// Battle party and star-map fleet are separate until sector/run progression links them.
		run.PlayerParty.Add(new Instance
		{
			Type = EType.Fighter,
			Alliance = Alliance.Player,
		});
		run.StarSystem = StarSystemOrchestrator.CreateDevSession(PlayerFleetUnitId, seed);
		return run;
	}
}

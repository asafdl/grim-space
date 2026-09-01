using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Generation;

namespace GrimSpace.Run;

public sealed class State
{
	public const string PlayerFleetUnitId = "player-fleet";

	public Party PlayerParty { get; } = new();
	public StarMap Map { get; set; } = null!;
	public StarSystemOrchestrator Traffic { get; set; } = null!;

	public static State CreateDevDefault(int seed = 0)
	{
		var run = new State();
		// Battle party and star-map fleet are separate until sector/run progression links them.
		run.PlayerParty.Add(new Instance
		{
			Type = EType.Fighter,
			Alliance = Alliance.Player,
		});
		(run.Map, run.Traffic) = StarSystemRunAssembly.Assemble(PlayerFleetUnitId, seed);
		return run;
	}
}

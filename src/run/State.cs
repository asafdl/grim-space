using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem;

namespace GrimSpace.Run;

public sealed class State
{
	public Party PlayerParty { get; } = new();
	public StarMap Map { get; set; } = null!;
	public StarSystemOrchestrator Traffic { get; set; } = null!;

	public static State CreateDevDefault(int seed = 0)
	{
		var run = new State();
		run.PlayerParty.Add(new Instance
		{
			Type = EType.Fighter,
			Alliance = Alliance.Player,
		});
		var buildResult = StarMap.CreateDevBuildResult(seed);
		run.Map = buildResult.Map;
		run.Traffic = StarSystemOrchestrator.FromBuildResult(buildResult);
		return run;
	}
}

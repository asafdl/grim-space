// Placeholder until roguelike sector map exists.

using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Run;

public sealed class State
{
	public Party PlayerParty { get; } = new();

	public static State CreateDevDefault()
	{
		var run = new State();
		run.PlayerParty.Add(new Instance
		{
			Id = "player",
			Type = EType.Fighter,
			Controller = EController.Player,
		});
		return run;
	}
}

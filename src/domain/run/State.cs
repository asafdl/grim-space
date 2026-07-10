using GrimSpace.Domain.Units;
using GrimSpace.Domain.Units.Enums;

namespace GrimSpace.Domain.Run;

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

using GrimSpace.Battle.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle;

public static class RulesEngine
{
	public readonly record struct Outcome(bool IsOver, string? WinnerId);

	public static Outcome Evaluate(IReadOnlyList<Unit> units)
	{
		var player = units.FirstOrDefault(u => u.Controller == EController.Player);
		var enemy = units.FirstOrDefault(u => u.Controller == EController.Enemy);

		if (enemy is not null && !enemy.State.IsAlive)
			return new Outcome(true, player?.State.Id);

		if (player is not null && !player.State.IsAlive)
			return new Outcome(true, enemy?.State.Id);

		return new Outcome(false, null);
	}
}

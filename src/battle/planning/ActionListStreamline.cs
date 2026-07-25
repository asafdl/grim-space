using GrimSpace.Battle.Board;
using GrimSpace.Battle.Runtime;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;

namespace GrimSpace.Battle.Planning;

public static class ActionListStreamline
{
	public static Simulation<BattleBoard, ActorSession> Apply(
		Engine<BattleBoard, ActorSession> engine,
		Simulation<BattleBoard, ActorSession> session,
		IReadOnlyList<IActionStreamline> streamliners)
	{
		var actions = session.Actions.ToList();
		foreach (var streamliner in streamliners)
			actions = streamliner.Streamline(actions).ToList();

		var fresh = engine.CreateSimulation();
		foreach (var action in actions)
		{
			if (!fresh.TryEnqueue(action))
				throw new InvalidOperationException("Failed to replay streamlined action onto a fresh simulation.");
		}

		return fresh;
	}
}

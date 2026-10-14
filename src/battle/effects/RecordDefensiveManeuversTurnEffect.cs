using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Effects;

public sealed class RecordDefensiveManeuversTurnEffect : IEffect<BattleWorld, ActorRuntime>
{
	private int? _previousMpSpent;

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var actor = world.StateOf(actorId);
		_previousMpSpent = actor.DefensiveManeuversMpSpentLastTurn;
		actor.DefensiveManeuversMpSpentLastTurn = System.Math.Max(
			0,
			actor.Maneuverability.MaxManeuverPoints - actor.ManeuverPoints);
		return [];
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId) =>
		world.StateOf(actorId).DefensiveManeuversMpSpentLastTurn = _previousMpSpent;
}

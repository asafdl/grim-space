using GrimSpace.Battle;
using GrimSpace.Battle.ActivationRound;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Effects;

public sealed class ScheduleActivationRoundEffect(
	EUnitActivationPhase runAt,
	int? roundsRemaining,
	bool sticky,
	IAction action) : IEffect<BattleWorld, ActorRuntime>
{
	private ActivationRoundEntry? _appended;

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var entry = new ActivationRoundEntry(runAt, roundsRemaining, sticky, action);
		world.ActivationRoundLists.Append(actorId, entry);
		_appended = entry;
		return [];
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		if (_appended is null)
			return;

		world.ActivationRoundLists.TryRemoveLastMatching(actorId, _appended);
		_appended = null;
	}
}

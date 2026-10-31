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
	IAction action,
	string? onUnitId = null) : IEffect<BattleWorld, ActorRuntime>
{
	private ActivationRoundEntry? _appended;
	private string? _scheduledOn;

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		_scheduledOn = onUnitId ?? actorId;
		var entry = new ActivationRoundEntry(runAt, roundsRemaining, sticky, action);
		world.ActivationRoundLists.Append(_scheduledOn, entry);
		_appended = entry;
		return [];
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		if (_appended is null || _scheduledOn is null)
			return;

		world.ActivationRoundLists.TryRemoveLastMatching(_scheduledOn, _appended);
		_appended = null;
		_scheduledOn = null;
	}
}

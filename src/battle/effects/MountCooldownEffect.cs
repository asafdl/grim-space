using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Battle.Effects;

public sealed class MountCooldownEffect(EAbilityKind kind, int cooldownTurns)
	: IEffect<BattleWorld, ActorRuntime>
{
	private int _previous;

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var runtimeState = world.StateOf(actorId).MountRuntimeFor(kind);
		_previous = runtimeState.CooldownRemaining;
		runtimeState.CooldownRemaining = cooldownTurns;
		return [];
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId) =>
		world.StateOf(actorId).MountRuntimeFor(kind).CooldownRemaining = _previous;
}

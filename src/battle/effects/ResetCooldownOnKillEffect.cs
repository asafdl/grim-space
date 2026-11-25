using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Battle.Effects;

public readonly record struct CooldownResetOnKillFacts(string ActorId, AbilityMount Mount);

public sealed class ResetCooldownOnKillEffect(string unitId, AbilityMount mount) : IEffect<BattleWorld, ActorRuntime>
{
	private int _previous;

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var counters = world.StateOf(unitId).MountRuntimeFor(mount);
		_previous = counters.CooldownRemaining;
		counters.CooldownRemaining = 0;
		return [new Record<CooldownResetOnKillFacts>(new(unitId, mount))];
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId) =>
		world.StateOf(unitId).MountRuntimeFor(mount).CooldownRemaining = _previous;
}

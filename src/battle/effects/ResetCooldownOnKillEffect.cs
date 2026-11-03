using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Battle.Effects;

public readonly record struct CooldownResetOnKillFacts(string ActorId, AbilityMount Mount);

public sealed class ResetCooldownOnKillEffect(IEffect<BattleWorld, ActorRuntime> damageEffect)
	: IEffect<BattleWorld, ActorRuntime>
{
	private readonly List<(string ActorId, AbilityMount Mount, int Cooldown)> _previous = [];

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		_previous.Clear();
		var aliveBefore = world.UnitRegistry.All
			.Where(unit => unit.State.IsAlive)
			.Select(unit => unit.State.Id)
			.ToHashSet(StringComparer.Ordinal);
		var records = damageEffect.Apply(world, runtime, actorId).ToList();
		var impacts = records.OfType<Record<ImpactFacts>>().Select(record => record.Value).ToArray();
		foreach (var impact in impacts)
		{
			if (!aliveBefore.Contains(impact.TargetId)
				|| world.StateOf(impact.TargetId).IsAlive
				|| impact.HullDamage <= 0)
				continue;

			var sourceId = impact.SourceId;
			var visited = new HashSet<string>(StringComparer.Ordinal);
			while (visited.Add(sourceId) && world.UnitRegistry.TryGet(sourceId, out var source))
			{
				if (source.State.IsAlive)
				{
					foreach (var installed in source.State.Loadout.InstalledAbilities)
					{
						if (installed.Spec is not ICooldownAbility { ResetsCooldownOnKill: true })
							continue;
						var counters = source.State.MountRuntimeFor(installed.Mount);
						if (counters.CooldownRemaining <= 0)
							continue;
						_previous.Add((sourceId, installed.Mount, counters.CooldownRemaining));
						counters.CooldownRemaining = 0;
						records.Add(new Record<CooldownResetOnKillFacts>(new(sourceId, installed.Mount)));
					}
				}
				sourceId = source.State.ParentId;
			}
		}
		return records;
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		foreach (var (sourceId, mount, cooldown) in _previous)
			world.StateOf(sourceId).MountRuntimeFor(mount).CooldownRemaining = cooldown;
		damageEffect.Undo(world, runtime, actorId);
	}
}

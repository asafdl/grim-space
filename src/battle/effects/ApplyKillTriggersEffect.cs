using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Battle.Effects;

public readonly record struct CooldownResetOnKillFacts(string ActorId, AbilityMount Mount);
public readonly record struct ThirstForScrapChangedFacts(string ActorId, int Stacks);

public sealed class ApplyKillTriggersEffect(IEffect<BattleWorld, ActorRuntime> damageEffect)
	: IEffect<BattleWorld, ActorRuntime>
{
	private readonly List<(string ActorId, AbilityMount Mount, int Cooldown)> _previous = [];
	private readonly Dictionary<string, int> _previousStacks = new(StringComparer.Ordinal);

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		_previous.Clear();
		_previousStacks.Clear();
		var aliveBefore = world.UnitRegistry.All
			.Where(unit => unit.State.IsAlive)
			.Select(unit => unit.State.Id)
			.ToHashSet(StringComparer.Ordinal);
		var records = damageEffect.Apply(world, runtime, actorId).ToList();
		var impacts = records.OfType<Record<ImpactFacts>>().Select(record => record.Value).ToArray();
		var creditedTargets = new HashSet<string>(StringComparer.Ordinal);
		foreach (var impact in impacts)
		{
			if (!aliveBefore.Contains(impact.TargetId)
				|| world.StateOf(impact.TargetId).IsAlive
				|| impact.HullDamage <= 0
				|| !creditedTargets.Add(impact.TargetId))
				continue;

			var sourceId = impact.SourceId;
			var visited = new HashSet<string>(StringComparer.Ordinal);
			while (visited.Add(sourceId) && world.UnitRegistry.TryGet(sourceId, out var source))
			{
				if (source.State.IsAlive)
				{
					if (source.RelationTo(world.UnitRegistry.UnitOf(impact.TargetId)) == EUnitRelation.Opponent
						&& BattleStatuses.HasThirstForScrap(source.State))
					{
						_previousStacks.TryAdd(sourceId, source.State.ThirstForScrapStacks);
						source.State.ThirstForScrapStacks++;
						records.Add(new Record<ThirstForScrapChangedFacts>(
							new(sourceId, source.State.ThirstForScrapStacks)));
					}

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
		foreach (var (sourceId, stacks) in _previousStacks)
			world.StateOf(sourceId).ThirstForScrapStacks = stacks;
		foreach (var (sourceId, mount, cooldown) in _previous)
			world.StateOf(sourceId).MountRuntimeFor(mount).CooldownRemaining = cooldown;
		damageEffect.Undo(world, runtime, actorId);
	}
}

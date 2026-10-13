using System.Collections.Frozen;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Skills;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Pilots;

namespace GrimSpace.Battle.Runtime;

public sealed class ActorRuntime : IRuntimeContext<ActorRuntime>
{
	private static readonly FrozenSet<EPilotSkillId> EmptySkills = FrozenSet<EPilotSkillId>.Empty;
	private static readonly IReadOnlyList<IActionEffectModifier<BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>> EmptyModifiers =
		Array.Empty<IActionEffectModifier<BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>>();

	private readonly IReadOnlyList<IActionEffectModifier<BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>> _modifiers;

	public ActorRuntime() : this(null, EmptySkills, EmptyModifiers) { }

	private ActorRuntime(
		string? pilotId,
		IReadOnlySet<EPilotSkillId> pilotSkills,
		IReadOnlyList<IActionEffectModifier<BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>> modifiers)
	{
		PilotId = pilotId;
		PilotSkills = pilotSkills;
		_modifiers = modifiers;
	}

	public string? PilotId { get; }

	public IReadOnlySet<EPilotSkillId> PilotSkills { get; }

	public static ActorRuntime From(PilotBattleSnapshot? snapshot)
	{
		if (snapshot is null)
			return new ActorRuntime(null, EmptySkills, EmptyModifiers);

		var skills = snapshot.SkillIds.Count == 0
			? EmptySkills
			: snapshot.SkillIds.ToFrozenSet();
		var modifiers = BuildModifiers(skills);
		return new ActorRuntime(snapshot.PilotId, skills, modifiers);
	}

	public void Reset() { }

	public ActorRuntime Fork() => this;

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> ApplyActionEffectModifiers(
		IAction action,
		BattleWorld world,
		IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> effects)
	{
		if (_modifiers.Count == 0)
			return effects;

		var modified = new List<IEffect<BattleWorld, ActorRuntime>>(effects.Count);
		foreach (var effect in effects)
		{
			var current = effect;
			foreach (var skill in _modifiers)
				current = skill.Modify(action, world, this, current);
			modified.Add(current);
		}

		return modified;
	}

	private static IReadOnlyList<IActionEffectModifier<BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>> BuildModifiers(
		IReadOnlySet<EPilotSkillId> skills)
	{
		if (!skills.Contains(EPilotSkillId.IonizedPayload))
			return EmptyModifiers;

		return [IonizedPayloadModifier.Instance];
	}
}

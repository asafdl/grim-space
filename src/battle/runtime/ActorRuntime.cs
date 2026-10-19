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

	public ActorRuntime() : this(null, null, EmptySkills, ActionEffectPipeline<BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>.Empty) { }

	private ActorRuntime(
		string? pilotId,
		string? portraitId,
		IReadOnlySet<EPilotSkillId> pilotSkills,
		ActionEffectPipeline<BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> actionEffects)
	{
		PilotId = pilotId;
		PortraitId = portraitId;
		PilotSkills = pilotSkills;
		ActionEffects = actionEffects;
	}

	public string? PilotId { get; }

	public string? PortraitId { get; }

	public IReadOnlySet<EPilotSkillId> PilotSkills { get; }

	public ActionEffectPipeline<BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> ActionEffects { get; }

	public static ActorRuntime From(PilotBattleSnapshot? snapshot)
	{
		if (snapshot is null)
			return new ActorRuntime(null, null, EmptySkills, ActionEffectPipeline<BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>.Empty);

		var skills = snapshot.SkillIds.Count == 0
			? EmptySkills
			: snapshot.SkillIds.ToFrozenSet();
		return new ActorRuntime(
			snapshot.PilotId,
			snapshot.PortraitId,
			skills,
			BattleActionEffectPipelineFactory.Build(skills));
	}

	public void Reset() { }

	public ActorRuntime Fork() => this;
}

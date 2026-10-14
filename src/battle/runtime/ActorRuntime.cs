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

	public ActorRuntime() : this(null, EmptySkills, ActionEffectPipeline<BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>.Empty) { }

	private ActorRuntime(
		string? pilotId,
		IReadOnlySet<EPilotSkillId> pilotSkills,
		ActionEffectPipeline<BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> actionEffects)
	{
		PilotId = pilotId;
		PilotSkills = pilotSkills;
		ActionEffects = actionEffects;
	}

	public string? PilotId { get; }

	public IReadOnlySet<EPilotSkillId> PilotSkills { get; }

	public ActionEffectPipeline<BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> ActionEffects { get; }

	public static ActorRuntime From(PilotBattleSnapshot? snapshot)
	{
		if (snapshot is null)
			return new ActorRuntime(null, EmptySkills, ActionEffectPipeline<BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>.Empty);

		var skills = snapshot.SkillIds.Count == 0
			? EmptySkills
			: snapshot.SkillIds.ToFrozenSet();
		return new ActorRuntime(snapshot.PilotId, skills, BattleActionEffectPipelineFactory.Build(skills));
	}

	public void Reset() { }

	public ActorRuntime Fork() => this;
}

using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Pilots;

namespace GrimSpace.Battle.PilotSkills;

public static class PilotSkillCapabilities
{
	public static IReadOnlyList<IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>> DefsFor(
		IReadOnlyList<EPilotSkillId> skills)
	{
		ArgumentNullException.ThrowIfNull(skills);
		if (skills.Count == 0)
			return Array.Empty<IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>>();

		var defs = new List<IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>>();
		var seen = new HashSet<IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>>();
		foreach (var skillId in skills)
		{
			var def = DefFor(skillId);
			if (def is not null && seen.Add(def))
				defs.Add(def);
		}

		return defs;
	}

	private static IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>? DefFor(
		EPilotSkillId skillId) =>
		skillId switch
		{
			EPilotSkillId.ShieldsUp => ShieldsUpDef.Instance,
			_ => null,
		};
}

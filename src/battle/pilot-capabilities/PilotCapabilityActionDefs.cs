using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Pilots;

namespace GrimSpace.Battle.PilotCapabilities;

public static class PilotCapabilityActionDefs
{
	public static IReadOnlyList<IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>> DefsFor(
		IReadOnlySet<EPilotCapabilityId> capabilities)
	{
		ArgumentNullException.ThrowIfNull(capabilities);
		if (capabilities.Count == 0)
			return Array.Empty<IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>>();

		var defs = new List<IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>>();
		var seen = new HashSet<IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>>();
		foreach (var capabilityId in capabilities.OrderBy(capability => capability))
		{
			var def = DefFor(capabilityId);
			if (def is not null && seen.Add(def))
				defs.Add(def);
		}

		return defs;
	}

	private static IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>? DefFor(
		EPilotCapabilityId capabilityId) =>
		capabilityId switch
		{
			EPilotCapabilityId.ShieldBoost => ShieldsUpDef.Instance,
			EPilotCapabilityId.OverchargeEngines => OverchargeEnginesDef.Instance,
			EPilotCapabilityId.ShieldBrace => BraceDef.Instance,
			_ => null,
		};
}

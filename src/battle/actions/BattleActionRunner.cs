using GrimSpace.Battle.Slices;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Actions;

public static class BattleActionRunner
{
	public static bool IsKnown(IAction action) => action switch
	{
		MoveStepAction or HeadingTurnAction or RollAction or MissileAction or FlakAction
			or RailgunAction or EndOfPhaseAction or RoundUpkeepAction or ResolveHazardAction
			or ClearTurnHazardsAction => true,
		_ => false,
	};

	public static IActionDef DefinitionFor(IAction action) => action switch
	{
		MoveStepAction => MoveDef.Instance,
		HeadingTurnAction => HeadingDef.Instance,
		RollAction => RollDef.Instance,
		MissileAction missile => MissileDef.For(missile.Mount, missile.Range),
		FlakAction flak => FlakDef.For(flak.Mount),
		RailgunAction => RailgunDef.Instance,
		EndOfPhaseAction => EndOfPhaseDef.Instance,
		RoundUpkeepAction => RoundUpkeepDef.Instance,
		ResolveHazardAction => ResolveHazardDef.Instance,
		ClearTurnHazardsAction => ClearTurnHazardsDef.Instance,
		_ => throw new ArgumentException($"Unknown battle action: {action.GetType().Name}", nameof(action)),
	};

	public static bool IsLegal(IAction action, BattleActionContext ctx) =>
		IsKnown(action) && DefinitionFor(action).IsLegal(action, ctx);

	public static void Apply(IAction action, BattleActionContext ctx)
	{
		foreach (var effect in DefinitionFor(action).Resolve(action, ctx))
			effect.Apply(ctx.Slices);
	}

	public static bool TryApply(IAction action, BattleActionContext ctx)
	{
		if (!IsLegal(action, ctx))
			return false;

		Apply(action, ctx);
		return true;
	}
}

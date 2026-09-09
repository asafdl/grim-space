using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Agents;
using GrimSpace.World.StarSystem.Contact;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Presentation;

internal static class ActionLegalityDiagnostics
{
	public static string DescribeAgentBlocked(StarMapPlayerExecutionAgent agent)
	{
		if (agent.IsCommittedForDiagnostics)
			return "already_committed";
		if (!agent.CanWorkForDiagnostics)
			return "agent_not_working";
		if (agent.ActorId is null)
			return "no_actor";
		return "agent_blocked";
	}

	public static string DescribeIllegality(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		if (action is not IAction<StarMap, ActorRuntime> typed)
			return "unsupported_action";

		if (typed.Definition.IsLegal(action, world, runtime))
			return "legal";

		return action switch
		{
			MoveAction move => DescribeMoveIllegality(move, world),
			HuntUnitAction hunt => DescribeHuntIllegality(hunt, world),
			EngageAction engage => DescribeEngageIllegality(engage, world),
			FleeAction flee => DescribeEngageIllegality(flee, world),
			AcceptContractAction accept => DescribeAcceptIllegality(accept, world),
			DeclineContractAction decline => DescribeDeclineIllegality(decline, world),
			_ => "illegal",
		};
	}

	private static string DescribeMoveIllegality(MoveAction move, StarMap world)
	{
		if (!world.UnitRegistry.TryGet(move.UnitId, out var unit))
			return "unit_missing";

		if (!unit.State.CanMove)
			return $"cannot_move phase={unit.State.Phase}";

		if (unit.State.EngagedWithUnitIds.Count > 0)
			return "engaged";

		if (IsWaitingForScheduledWork(world, unit.State))
			return "scheduled_work_pending";

		return "illegal";
	}

	private static string DescribeHuntIllegality(HuntUnitAction hunt, StarMap world)
	{
		if (hunt.ActorId == hunt.TargetUnitId)
			return "self_target";

		if (!world.UnitRegistry.TryGet(hunt.ActorId, out var initiator))
			return "actor_missing";

		if (!world.UnitRegistry.TryGet(hunt.TargetUnitId, out var target))
			return "target_missing";

		if (!initiator.State.CanMove)
			return $"cannot_move phase={initiator.State.Phase}";

		if (target.State.CombatProfile is null)
			return "target_not_combatant";

		return "illegal";
	}

	private static string DescribeEngageIllegality(IAction action, StarMap world)
	{
		if (!world.UnitRegistry.TryGet(action.ActorId, out var actor))
			return "actor_missing";

		var state = actor.State;
		if (state.EngagementPhase != EEngagementPhase.AwaitingDecision)
			return $"wrong_engagement_phase phase={state.EngagementPhase}";

		var counterpartyId = EngagementQueries.ResolveCounterpartyId(state);
		if (counterpartyId is null)
			return "no_counterparty";

		if (!world.UnitRegistry.TryGet(counterpartyId, out var counterparty))
			return "counterparty_missing";

		if (counterparty.State.HuntedByUnitId != action.ActorId
			&& state.HuntedByUnitId != counterpartyId)
			return "not_in_hunt_range";

		return "illegal";
	}

	private static string DescribeAcceptIllegality(AcceptContractAction accept, StarMap world)
	{
		if (!world.ContractRegistry.TryGet(accept.ContractId, out _))
			return "contract_missing";

		if (!world.ContractRegistry.IsOffered(accept.ContractId))
			return "contract_not_offered";

		if (!world.UnitRegistry.TryGet(accept.ActorId, out _))
			return "actor_missing";

		return "illegal";
	}

	private static string DescribeDeclineIllegality(DeclineContractAction decline, StarMap world)
	{
		if (!world.ContractRegistry.TryGet(decline.ContractId, out _))
			return "contract_missing";

		if (!world.ContractRegistry.IsOffered(decline.ContractId))
			return "contract_not_offered";

		if (!world.UnitRegistry.TryGet(decline.ActorId, out _))
			return "actor_missing";

		return "illegal";
	}

	private static bool IsWaitingForScheduledWork(StarMap world, State state) =>
		state.ChoreDockIds.Count > 0
		&& world.Timeline.ContainsPending(action =>
			action is BeginWorkAction begin && begin.UnitId == state.Id);
}

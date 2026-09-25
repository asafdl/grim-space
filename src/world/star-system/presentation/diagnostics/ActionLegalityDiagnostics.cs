using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Agents;
using GrimSpace.World.StarSystem.Contact;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Presentation.Diagnostics;

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
			PursueContactAction pursue => DescribePursueIllegality(pursue, world),
			EngageAction engage => DescribeEngageIllegality(engage, world),
			FleeAction flee => DescribeEngageIllegality(flee, world),
			AcceptContractAction accept => DescribeAcceptIllegality(accept, world),
			DeclineContractAction decline => DescribeDeclineIllegality(decline, world),
			ReachWreckageAction reach => DescribeReachWreckIllegality(reach, world),
			LeaveWreckageAction leave => DescribeLeaveWreckIllegality(leave, world),
			InvestigateWreckageAction investigate => DescribeInvestigateWreckIllegality(investigate, world),
			_ => "illegal",
		};
	}

	private static string DescribeMoveIllegality(MoveAction move, StarMap world)
	{
		if (!world.FleetRegistry.TryGet(move.UnitId, out var unit))
			return "unit_missing";

		if (!unit.State.CanMove)
			return $"cannot_move phase={unit.State.Phase}";

		if (EngagementState.IsEngaged(unit.State))
			return "engaged";

		if (IsWaitingForScheduledWork(world, unit.State))
			return "scheduled_work_pending";

		return "illegal";
	}

	private static string DescribePursueIllegality(PursueContactAction pursue, StarMap world)
	{
		if (!world.FleetRegistry.TryGet(pursue.ActorId, out var initiator))
			return "actor_missing";

		if (!initiator.State.CanMove)
			return $"cannot_move phase={initiator.State.Phase}";

		return pursue.Target switch
		{
			FleetContactTarget fleet when pursue.ActorId == fleet.UnitId => "self_target",
			FleetContactTarget fleet when !world.FleetRegistry.TryGet(fleet.UnitId, out var target) =>
				"target_missing",
			FleetContactTarget fleet when world.FleetRegistry.TryGet(fleet.UnitId, out var target)
				&& target.State.CombatProfile is null => "target_not_combatant",
			WreckContactTarget wreck when !world.ContractRegistry.TryGet(wreck.ContractId, out _) =>
				"contract_missing",
			WreckContactTarget wreck
				when world.ContractRegistry.TryGet(wreck.ContractId, out var contract)
				&& contract.Objective is not WreckageObjective => "not_wreckage_contract",
			_ => "illegal",
		};
	}

	private static string DescribeEngageIllegality(IAction action, StarMap world)
	{
		if (!world.FleetRegistry.TryGet(action.ActorId, out var actor))
			return "actor_missing";

		var state = actor.State;
		if (state.CurrentEngagement?.Phase != EEngagementPhase.AwaitingDecision)
			return $"wrong_engagement_phase phase={EngagementState.Phase(state)}";

		var counterpartyId = EngagementQueries.ResolveCounterpartyId(state);
		if (counterpartyId is null)
			return "no_counterparty";

		if (!world.FleetRegistry.TryGet(counterpartyId, out var counterparty))
			return "counterparty_missing";

		if (!EngagementState.HasMutualHuntLink(state, counterparty.State))
			return "not_in_hunt_range";

		return "illegal";
	}

	private static string DescribeAcceptIllegality(AcceptContractAction accept, StarMap world)
	{
		if (!world.FleetRegistry.TryGet(accept.ActorId, out _))
			return "actor_missing";

		if (!world.ContractRegistry.TryGet(accept.ContractId, out _))
			return "contract_missing";

		if (!world.ContractRegistry.IsPending(accept.ContractId))
			return "contract_not_pending";

		return "illegal";
	}

	private static string DescribeReachWreckIllegality(ReachWreckageAction reach, StarMap world)
	{
		if (!world.FleetRegistry.TryGet(reach.ActorId, out var unit))
			return "actor_missing";

		if (!string.IsNullOrEmpty(unit.State.PendingWreckContractId))
			return "wreck_decision_pending";

		if (!WreckageQueries.IsActiveWreckContractForHolder(world, reach.ActorId, reach.ContractId))
			return "invalid_wreck_contract";

		return "illegal";
	}

	private static string DescribeLeaveWreckIllegality(LeaveWreckageAction leave, StarMap world)
	{
		if (!world.FleetRegistry.TryGet(leave.ActorId, out var unit))
			return "actor_missing";

		if (string.IsNullOrEmpty(unit.State.PendingWreckContractId))
			return "no_pending_wreck";

		return "illegal";
	}

	private static string DescribeInvestigateWreckIllegality(
		InvestigateWreckageAction investigate,
		StarMap world)
	{
		if (!world.FleetRegistry.TryGet(investigate.ActorId, out var unit))
			return "actor_missing";

		if (unit.State.PendingWreckContractId != investigate.ContractId)
			return "wreck_decision_mismatch";

		if (!WreckageQueries.IsActiveWreckContractForHolder(world, investigate.ActorId, investigate.ContractId))
			return "invalid_wreck_contract";

		return "illegal";
	}

	private static string DescribeDeclineIllegality(DeclineContractAction decline, StarMap world)
	{
		if (!world.FleetRegistry.TryGet(decline.ActorId, out _))
			return "actor_missing";

		if (!world.ContractRegistry.TryGet(decline.ContractId, out var contract))
			return "contract_missing";

		if (!world.ContractRegistry.IsPending(decline.ContractId))
			return "contract_not_pending";

		if (!contract.AllowsDecline)
			return "decline_not_allowed";

		return "illegal";
	}

	private static bool IsWaitingForScheduledWork(StarMap world, State state) =>
		state.ChoreDockIds.Count > 0
		&& world.Timeline.ContainsPending(action =>
			action is BeginWorkAction begin && begin.UnitId == state.Id);
}

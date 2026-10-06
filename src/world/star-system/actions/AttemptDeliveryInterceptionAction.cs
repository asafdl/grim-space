using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.FleetSpawner;
using GrimSpace.World.StarSystem.Ids;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record AttemptDeliveryInterceptionAction(
	string ActorId,
	string ContractId,
	string PlayerFleetId) : IAction<StarMap, ActorRuntime>
{
	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		AttemptDeliveryInterceptionDef.Instance;
}

public sealed class AttemptDeliveryInterceptionDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static AttemptDeliveryInterceptionDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) =>
		action is AttemptDeliveryInterceptionAction attempt
		&& string.Equals(attempt.ActorId, StarSystemActorIds.Contracts, StringComparison.Ordinal)
		&& world.ContractRegistry.TryGetState(attempt.ContractId, out var state)
		&& state.Status == EContractStatus.Active
		&& state is DeliveryContractState delivery
		&& delivery.Progress.InterceptionState == EDeliveryInterceptionState.Pending
		&& world.ContractRegistry.TryGet(attempt.ContractId, out var contract)
		&& contract.Objective is DeliveryObjective;

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		if (action is not AttemptDeliveryInterceptionAction attempt
			|| !IsLegal(action, world, runtime)
			|| !world.ContractRegistry.TryGet(attempt.ContractId, out var contract)
			|| contract.Objective is not DeliveryObjective deliveryObjective)
			return [];

		var retryDelayTicks = deliveryObjective.Config.InterceptionRetryDelayTicks;
		if (!world.FleetRegistry.TryGet(attempt.PlayerFleetId, out var player)
			|| player.State.Phase != EPhase.InTransit)
		{
			return
			[
				new RescheduleDeliveryInterceptionAttemptEffect(
					attempt,
					EDeliveryInterceptionRetryReason.PlayerNotInTransit,
					retryDelayTicks),
			];
		}

		if (player.State.CurrentEngagement is not null)
		{
			return
			[
				new RescheduleDeliveryInterceptionAttemptEffect(
					attempt,
					EDeliveryInterceptionRetryReason.PlayerEngaged,
					retryDelayTicks),
			];
		}

		if (!TrySelectInterceptor(world, attempt.PlayerFleetId, out var interceptorFleetId))
		{
			return
			[
				new RescheduleDeliveryInterceptionAttemptEffect(
					attempt,
					EDeliveryInterceptionRetryReason.NoEligibleInterceptor,
					retryDelayTicks),
			];
		}

		return
		[
			new AssignDeliveryInterceptorEffect(
				attempt.ContractId,
				interceptorFleetId,
				attempt.PlayerFleetId),
		];
	}

	internal static bool TrySelectInterceptor(
		StarMap world,
		string playerFleetId,
		out string interceptorFleetId)
	{
		interceptorFleetId = "";
		if (!world.FleetRegistry.TryGet(playerFleetId, out var player))
			return false;

		var pathfinder = new GridPathfinder(world.PathfindingTerrain);
		if (!TryResolvePosition(world, pathfinder, player, out var playerPosition))
			return false;

		var bestDistance = int.MaxValue;
		string? bestFleetId = null;
		foreach (var fleet in world.FleetRegistry.All)
		{
			if (!IsEligibleAmbientInterceptor(fleet.State)
				|| !TryResolvePosition(world, pathfinder, fleet, out var origin)
				|| pathfinder.FindPath(origin, playerPosition) is not PathfindingResult.Found)
				continue;

			var distance = origin.ManhattanDistanceTo(playerPosition);
			if (distance > bestDistance
				|| distance == bestDistance
				&& bestFleetId is not null
				&& string.CompareOrdinal(fleet.State.Id, bestFleetId) >= 0)
				continue;

			bestDistance = distance;
			bestFleetId = fleet.State.Id;
		}

		if (bestFleetId is null)
			return false;

		interceptorFleetId = bestFleetId;
		return true;
	}

	private static bool TryResolvePosition(
		StarMap world,
		GridPathfinder pathfinder,
		Fleet fleet,
		out Coord position)
	{
		var state = fleet.State;
		if (state.Phase != EPhase.InTransit || !state.Journey.IsActive)
		{
			position = state.CommittedPosition(world, null, 0).Position;
			return true;
		}

		if (pathfinder.FindPath(state.Journey.Origin, state.Journey.Destination)
			is not PathfindingResult.Found found)
		{
			position = default;
			return false;
		}

		position = state.CommittedPosition(world, found.Path, 0).Position;
		return true;
	}

	internal static bool IsEligibleAmbientInterceptor(State interceptor) =>
		interceptor.Type == EType.PirateFleet
		&& interceptor.SpawnerSource == EFleetSpawnerSource.RandomArea
		&& string.IsNullOrEmpty(interceptor.SourceContractId)
		&& interceptor.PursuitDirective is null
		&& interceptor.CurrentEngagement is null
		&& interceptor.CanMove;
}

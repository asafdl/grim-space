using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Effects;

public sealed class CleanupDismissedContractEffect(
	string contractId,
	string holderUnitId) : IEffect<StarMap, ActorRuntime>
{
	private readonly List<Fleet> _removedFleets = [];
	private TravelTarget _previousTravelTarget;
	private string _previousPendingWreckContractId = "";
	private Engagement? _previousEngagement;
	private bool _previousWaitingForPlayerInput;

	public IReadOnlyList<IRecord> Apply(StarMap world, ActorRuntime runtime, string actorId)
	{
		if (!world.ContractRegistry.TryGet(contractId, out var contract)
			|| !world.ContractRegistry.TryGetState(contractId, out var state)
			|| state.HolderUnitId != holderUnitId)
			return [];

		_previousWaitingForPlayerInput = world.WaitingForPlayerInput;
		if (world.FleetRegistry.TryGet(holderUnitId, out var holder))
		{
			_previousTravelTarget = holder.State.TravelTarget;
			_previousPendingWreckContractId = holder.State.PendingWreckContractId;
			_previousEngagement = holder.State.CurrentEngagement;
			holder.State.TravelTarget = TravelTarget.None;
			if (holder.State.PendingWreckContractId == contractId)
				holder.State.PendingWreckContractId = "";
			if (ShouldClearEngagement(holder.State.CurrentEngagement, contract))
				holder.State.CurrentEngagement = null;
		}

		foreach (var fleetId in SpawnedFleetIds(contract))
		{
			if (string.Equals(fleetId, holderUnitId, StringComparison.Ordinal))
				continue;

			if (world.FleetRegistry.TryGet(fleetId, out var fleet))
			{
				if (fleet.State.Faction == GrimSpace.World.Factions.EFaction.Player)
					continue;

				_removedFleets.Add(fleet);
				world.FleetRegistry.Remove(fleetId);
			}
		}

		world.WaitingForPlayerInput = false;
		return [];
	}

	public void Undo(StarMap world, ActorRuntime runtime, string actorId)
	{
		foreach (var fleet in _removedFleets)
			world.FleetRegistry.Add(fleet);

		if (world.FleetRegistry.TryGet(holderUnitId, out var holder))
		{
			holder.State.TravelTarget = _previousTravelTarget;
			holder.State.PendingWreckContractId = _previousPendingWreckContractId;
			holder.State.CurrentEngagement = _previousEngagement;
		}

		world.WaitingForPlayerInput = _previousWaitingForPlayerInput;
		_removedFleets.Clear();
	}

	private static IEnumerable<string> SpawnedFleetIds(Contract contract)
	{
		switch (contract.Objective)
		{
			case HuntObjective hunt:
				foreach (var group in hunt.SpawnGroups)
				{
					for (var index = 0; index < group.RequiredCount; index++)
						yield return $"{contract.Id}.{group.GroupId}.{index}";
				}
				break;
			case WreckageObjective wreckage:
				yield return InvestigateWreckageDef.AmbushUnitIdFor(wreckage);
				break;
		}
	}

	private static bool ShouldClearEngagement(Engagement? engagement, Contract contract) =>
		engagement?.Hunting is { } targetId
		&& SpawnedFleetIds(contract).Contains(targetId, StringComparer.Ordinal);
}

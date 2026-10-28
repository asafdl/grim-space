using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Units;
using GrimSpace.World.StarSystem.Economy;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Effects;

public static class TravelFuelEffects
{
	public static IReadOnlyList<IEffect<StarMap, ActorRuntime>> ChargeWhileJourneying(
		StarMap world,
		ActorRuntime runtime,
		string playerFleetId)
	{
		if (!IsPlayerFleet(world, playerFleetId)
			|| !TrySampleJourney(
				world,
				runtime,
				playerFleetId,
				out var journey,
				out var distanceAlong))
			return [];

		return BuildChargeEffects(
			world,
			runtime,
			playerFleetId,
			journey.Id,
			distanceAlong,
			persistLedger: true,
			allowStarvation: true);
	}

	public static IReadOnlyList<IEffect<StarMap, ActorRuntime>> SettleBeforeStop(
		StarMap world,
		ActorRuntime runtime,
		string playerFleetId)
	{
		if (!IsPlayerFleet(world, playerFleetId))
			return [];

		if (!TrySampleJourney(
			world,
			runtime,
			playerFleetId,
			out var journey,
			out var distanceAlong))
			return [];

		return BuildChargeEffects(
			world,
			runtime,
			playerFleetId,
			journey.Id,
			distanceAlong,
			persistLedger: false,
			allowStarvation: false);
	}

	public static int EstimateRemainingFuelUnits(
		StarMap world,
		ActorRuntime runtime,
		string playerFleetId,
		TransitPath? pendingPath = null)
	{
		ArgumentNullException.ThrowIfNull(world);
		ArgumentNullException.ThrowIfNull(runtime);

		if (pendingPath is not null)
		{
			return TravelFuelAccounting.FuelUnitsForDistance(
				pendingPath.TotalLength + System.Math.Max(0, runtime.FuelUnchargedDistance));
		}

		if (!TrySampleJourney(
			world,
			runtime,
			playerFleetId,
			out _,
			out var distanceAlong)
			|| runtime.CachedPath is not { } path)
			return TravelFuelAccounting.FuelUnitsForDistance(
				System.Math.Max(0, runtime.FuelUnchargedDistance));

		var remaining = System.Math.Max(0, path.TotalLength - distanceAlong);
		return TravelFuelAccounting.FuelUnitsForDistance(
			remaining + System.Math.Max(0, runtime.FuelUnchargedDistance));
	}

	private static bool IsPlayerFleet(StarMap world, string fleetId) =>
		world.FleetRegistry.TryGet(fleetId, out var fleet)
		&& fleet.State.Type == EType.PlayerFleet;

	private static IReadOnlyList<IEffect<StarMap, ActorRuntime>> BuildChargeEffects(
		StarMap world,
		ActorRuntime runtime,
		string playerFleetId,
		long journeyId,
		double distanceAlong,
		bool persistLedger,
		bool allowStarvation)
	{
		var availableFuel = world.PlayerResources.GetBalance(ResourceId.Fuel);
		var charge = TravelFuelAccounting.Charge(
			new TravelFuelLedgerState(
				runtime.FuelLedgerJourneyId,
				runtime.FuelChargedDistanceAlongCurrentJourney,
				runtime.FuelUnchargedDistance),
			journeyId,
			distanceAlong,
			availableFuel);

		var effects = new List<IEffect<StarMap, ActorRuntime>>();
		if (charge.FuelToBurn > 0)
		{
			effects.Add(
				new ChangeResourceEffect(
					TransactionSource.TravelFuel,
					ResourceBundle.Of(ResourceId.Fuel, -charge.FuelToBurn)));
		}

		if (persistLedger)
			effects.Add(new UpdateTravelFuelLedgerEffect(charge.Ledger));

		if (allowStarvation
			&& charge.Starving
			&& TryBuildStarvationDamage(world, playerFleetId, out var damage))
			effects.Add(new RecordTravelStarvationDamageEffect(damage));

		return effects;
	}

	private static bool TrySampleJourney(
		StarMap world,
		ActorRuntime runtime,
		string playerFleetId,
		out FleetTravel.Journey journey,
		out double distanceAlong)
	{
		journey = null!;
		distanceAlong = 0;
		if (!world.FleetRegistry.TryGet(playerFleetId, out var fleet)
			|| fleet.State.Travel is not FleetTravel.Journey active
			|| runtime.CachedPath is not { } path)
			return false;

		journey = active;
		var elapsed = world.Timeline.Clock.Current - active.StartTick;
		distanceAlong = path.DistanceTraveledAtElapsed(elapsed, fleet.State.SpeedPerTick);
		return true;
	}

	private static bool TryBuildStarvationDamage(
		StarMap world,
		string playerFleetId,
		out TravelStarvationDamage damage)
	{
		damage = null!;
		if (world.ItemInventory is null
			|| !world.FleetRegistry.TryGet(playerFleetId, out var fleet))
			return false;

		var ships = new List<TravelStarvationShipDamage>();
		foreach (var member in fleet.Members)
		{
			if (!world.ItemInventory.TryGet(member.Id, out var before))
				continue;

			var afterHull = System.Math.Max(
				0,
				before.HullPoints - StrategicEconomyConfig.StarvationHullDamagePerTick);
			if (afterHull == before.HullPoints)
				continue;

			var after = new ShipInstance(
				before.Id,
				before.Spec,
				before.Loadout,
				afterHull,
				before.ShieldPoints.Clone());
			ships.Add(
				new TravelStarvationShipDamage(
					before.Id,
					before.Clone(),
					after));
		}

		if (ships.Count == 0)
			return false;

		damage = new TravelStarvationDamage(ships);
		return true;
	}
}

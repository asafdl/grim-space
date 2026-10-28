using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Economy;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Effects;

public sealed class UpdateTravelFuelLedgerEffect(TravelFuelLedgerState ledger)
	: IEffect<StarMap, ActorRuntime>
{
	private TravelFuelLedgerState _previous;

	public IReadOnlyList<IRecord> Apply(StarMap world, ActorRuntime runtime, string actorId)
	{
		_previous = new TravelFuelLedgerState(
			runtime.FuelLedgerJourneyId,
			runtime.FuelChargedDistanceAlongCurrentJourney,
			runtime.FuelUnchargedDistance);
		runtime.ApplyTravelFuelLedger(
			ledger.JourneyId,
			ledger.ChargedDistanceAlongCurrentJourney,
			ledger.UnchargedDistance);
		return [];
	}

	public void Undo(StarMap world, ActorRuntime runtime, string actorId) =>
		runtime.ApplyTravelFuelLedger(
			_previous.JourneyId,
			_previous.ChargedDistanceAlongCurrentJourney,
			_previous.UnchargedDistance);
}

using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Run;
using GrimSpace.Units;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Effects;

public sealed class EnlistPlayerShipEffect(ShipSpawnDeclaration declaration) : IEffect<StarMap, ActorRuntime>
{
	public IReadOnlyList<IRecord> Apply(StarMap world, ActorRuntime runtime, string actorId)
	{
		var fleet = world.FleetRegistry.FleetOf(actorId);
		var members = fleet.Members.Concat([new FleetMember(declaration.ShipId)]).ToArray();
		var registrations = fleet.Registrations.Concat([declaration]).ToArray();
		world.FleetRegistry.Replace(new Fleet(fleet.State, members, registrations));
		return [new Record<PlayerShipEnlisted>(new PlayerShipEnlisted(declaration))];
	}

	public void Undo(StarMap world, ActorRuntime runtime, string actorId)
	{
	}
}

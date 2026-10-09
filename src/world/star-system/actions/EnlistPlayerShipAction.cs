using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Units;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record EnlistPlayerShipAction(string ActorId, ShipSpawnDeclaration Declaration)
	: IAction<StarMap, ActorRuntime>
{
	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		EnlistPlayerShipActionDef.Instance;
}

public sealed class EnlistPlayerShipActionDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static EnlistPlayerShipActionDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime)
	{
		if (action is not EnlistPlayerShipAction enlist)
			return false;
		if (!world.FleetRegistry.TryGet(enlist.ActorId, out var fleet))
			return false;

		var shipId = enlist.Declaration.ShipId;
		if (fleet.Members.Any(member => string.Equals(member.Id, shipId, StringComparison.Ordinal)))
			return false;
		if (fleet.State.CurrentEngagement?.Phase == EEngagementPhase.Engaged)
			return false;
		if (world.FleetRegistry.TryFleetContainingMember(shipId, out _))
			return false;

		return true;
	}

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		var enlist = (EnlistPlayerShipAction)action;
		return [new EnlistPlayerShipEffect(enlist.Declaration)];
	}
}

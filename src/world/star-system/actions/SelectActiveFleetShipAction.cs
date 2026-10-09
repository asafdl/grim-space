using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record SelectActiveFleetShipAction(string ActorId, string ShipId)
	: IAction<StarMap, ActorRuntime>
{
	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		SelectActiveFleetShipActionDef.Instance;
}

public sealed class SelectActiveFleetShipActionDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static SelectActiveFleetShipActionDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime)
	{
		if (action is not SelectActiveFleetShipAction select)
			return false;
		if (!world.FleetRegistry.TryGet(select.ActorId, out var fleet))
			return false;

		return fleet.Members.Any(member =>
			string.Equals(member.Id, select.ShipId, StringComparison.Ordinal));
	}

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		var select = (SelectActiveFleetShipAction)action;
		if (!IsLegal(select, world, runtime))
			return [];

		return [new SelectActiveFleetShipEffect(select.ShipId)];
	}
}

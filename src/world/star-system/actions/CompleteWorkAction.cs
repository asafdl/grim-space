using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record CompleteWorkAction(
	string ActorId,
	string UnitId,
	string PoiId,
	int StartTick) : IAction<StarMap, ActorRuntime>
{
	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		CompleteWorkDef.Instance;
}

public sealed class CompleteWorkDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static CompleteWorkDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) =>
		action is CompleteWorkAction complete
		&& world.FleetRegistry.TryGet(complete.UnitId, out var unit)
		&& world.DockAt(unit.State)?.PoiId == complete.PoiId
		&& world.Timeline.Clock.Current
			== complete.StartTick
				+ world.GetPointOfInterest(complete.PoiId).DurationTicks(unit.State.Type);

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime) => [];
}

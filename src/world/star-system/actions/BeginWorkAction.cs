using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record BeginWorkAction(
	string ActorId,
	string UnitId,
	string PoiId,
	int StartTick) : IAction<StarMap, ActorRuntime>
{
	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		BeginWorkDef.Instance;
}

public sealed class BeginWorkDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static BeginWorkDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) =>
		action is BeginWorkAction begin
		&& world.FleetRegistry.TryGet(begin.UnitId, out var unit)
		&& world.DockAt(unit.State)?.PoiId == begin.PoiId
		&& WorkScheduler.IsAssigned(
			world,
			begin.UnitId,
			begin.PoiId,
			begin.StartTick);

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime) => [];
}

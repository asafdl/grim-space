using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Pathfinding;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record BeginTransitAction(
	string ActorId,
	string UnitId,
	string DestinationDockId,
	TransitPath Path) : IAction<StarMap, EmptyRuntime>
{
	public IActionDef<IAction, StarMap, EmptyRuntime, IEffect<StarMap, EmptyRuntime>> Definition =>
		BeginTransitDef.Instance;
}

public sealed class BeginTransitDef
	: IActionDef<IAction, StarMap, EmptyRuntime, IEffect<StarMap, EmptyRuntime>>
{
	public static BeginTransitDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, EmptyRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, EmptyRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, EmptyRuntime runtime) =>
		action is BeginTransitAction begin
		&& world.UnitRegistry.TryGet(begin.UnitId, out var unit)
		&& unit.State.IsReadyToDepart;

	public IReadOnlyList<IEffect<StarMap, EmptyRuntime>> Resolve(
		IAction action,
		StarMap world,
		EmptyRuntime runtime)
	{
		var begin = (BeginTransitAction)action;
		return [new BeginTransitEffect(begin.UnitId, begin.DestinationDockId, begin.Path)];
	}
}

using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;

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
		&& world.UnitRegistry.TryGet(complete.UnitId, out var unit)
		&& unit.State.Phase == EPhase.Working
		&& unit.State.WorkStartTick == complete.StartTick
		&& world.DocksById[unit.State.DockedAtDockId].PoiId == complete.PoiId;

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		var complete = (CompleteWorkAction)action;
		if (!world.UnitRegistry.TryGet(complete.UnitId, out var unit)
			|| unit.State.Phase != EPhase.Working
			|| unit.State.WorkStartTick != complete.StartTick
			|| world.DocksById[unit.State.DockedAtDockId].PoiId != complete.PoiId)
		{
			return [];
		}

		return [CompleteWorkEffect.Instance(complete.UnitId)];
	}
}

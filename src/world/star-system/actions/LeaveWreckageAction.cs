using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record LeaveWreckageAction(string ActorId) : IAction<StarMap, ActorRuntime>
{
	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		LeaveWreckageDef.Instance;
}

public sealed class LeaveWreckageDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static LeaveWreckageDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) =>
		action is LeaveWreckageAction leave
		&& world.FleetRegistry.TryGet(leave.ActorId, out var unit)
		&& !string.IsNullOrEmpty(unit.State.PendingWreckContractId);

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		var leave = (LeaveWreckageAction)action;
		return
		[
			new ClearPendingWreckContractEffect(leave.ActorId),
			new PlayerInputEffect(false),
		];
	}
}

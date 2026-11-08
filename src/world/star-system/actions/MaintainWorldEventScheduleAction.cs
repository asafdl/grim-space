using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Events;
using GrimSpace.World.StarSystem.Ids;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record WorldEventSlot(EWorldEventId EventId, int DelayTicks, ulong RngSeed);

public sealed record MaintainWorldEventScheduleAction(
	string ActorId,
	int Tick,
	IReadOnlyList<WorldEventSlot> Slots) : IAction<StarMap, ActorRuntime>
{
	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		MaintainWorldEventScheduleDef.Instance;
}

public sealed class MaintainWorldEventScheduleDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static MaintainWorldEventScheduleDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) =>
		action is MaintainWorldEventScheduleAction maintain
		&& string.Equals(maintain.ActorId, StarSystemActorIds.WorldEvents, StringComparison.Ordinal)
		&& maintain.Tick == world.Timeline.Clock.Current
		&& WorldEventContexts.FieldedPilots(world).Any()
		&& maintain.Slots.Count > 0
		&& maintain.Slots.All(slot =>
			slot.DelayTicks is >= 1 and <= WorldEventConfig.WakeCadenceTicks
			&& Enum.IsDefined(slot.EventId))
		&& world.Timeline.CountPending(pending => pending is IWorldEventRoll) + maintain.Slots.Count
			<= WorldEventConfig.TargetScheduledRolls;

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		var maintain = (MaintainWorldEventScheduleAction)action;
		return maintain.Slots
			.Select(slot => (IEffect<StarMap, ActorRuntime>)new ScheduleWorldEventActionEffect(
				slot.DelayTicks,
				WorldEventCatalog.CreateRoll(slot.EventId, slot.RngSeed)))
			.ToArray();
	}
}

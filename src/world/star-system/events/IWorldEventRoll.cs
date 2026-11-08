using GrimSpace.Core.Actions;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Events;

/// <summary>
/// Initiating action of a world event. Scheduled on the timeline by the event agent;
/// odds, context checks, and outcomes are resolved when it commits, not when it is scheduled.
/// </summary>
public interface IWorldEventRoll : IAction<StarMap, ActorRuntime>
{
}

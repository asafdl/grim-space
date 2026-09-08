using GrimSpace.Core.Actions;

namespace GrimSpace.Core.Engine;

public sealed class ActionBatch
{
	public ActionBatch(string actorId, IReadOnlyList<IAction> actions)
	{
		ArgumentException.ThrowIfNullOrEmpty(actorId);
		ArgumentNullException.ThrowIfNull(actions);

		ActorId = actorId;
		Actions = actions;
	}

	public string ActorId { get; }

	public IReadOnlyList<IAction> Actions { get; }
}

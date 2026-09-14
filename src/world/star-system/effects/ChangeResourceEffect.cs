using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Effects;

public sealed class ChangeResourceEffect(string source, ResourceBundle change) : IEffect<StarMap, ActorRuntime>
{
	private ResourceInventory? _snapshot;

	public IReadOnlyList<IRecord> Apply(StarMap world, ActorRuntime runtime, string actorId)
	{
		if (change.IsEmpty)
			return [];

		_snapshot = world.PlayerResources.Snapshot();
		if (!world.PlayerResources.TryApply(change))
		{
			_snapshot = null;
			return [];
		}

		return [new Record<Transaction>(new Transaction(source, change))];
	}

	public void Undo(StarMap world, ActorRuntime runtime, string actorId)
	{
		if (_snapshot is null)
			throw new InvalidOperationException("ChangeResourceEffect was not applied.");

		world.PlayerResources.Restore(_snapshot);
		_snapshot = null;
	}
}

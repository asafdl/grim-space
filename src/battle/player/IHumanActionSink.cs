using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Player;

public interface IHumanActionSink
{
	bool TryEnqueue(IReadOnlyList<IAction> actions);

	bool Undo();

	bool Commit();
}

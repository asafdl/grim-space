namespace GrimSpace.Core.Actions;

public interface IActionSink
{
	bool TryEnqueue(IReadOnlyList<IAction> actions);

	bool Undo();

	bool Commit();
}

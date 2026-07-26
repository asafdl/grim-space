namespace GrimSpace.Core.Actions;

public interface IActionStreamline
{
	IReadOnlyList<IAction> Streamline(IReadOnlyList<IAction> actions, IReadOnlyList<int?> undoGroups);
}

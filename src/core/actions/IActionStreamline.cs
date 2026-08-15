namespace GrimSpace.Core.Actions;

public interface IActionStreamline
{
	/// <summary>
	/// With <paramref name="input"/>, applies one button press and cycles to the next legal net orientation.
	/// With null <paramref name="input"/>, compacts the queue for commit.
	/// Returns null when no legal queue exists.
	/// </summary>
	IReadOnlyList<IAction>? Streamline(
		IReadOnlyList<IAction> queue,
		IAction? input,
		Func<IReadOnlyList<IAction>, bool> isCandidateLegal);
}

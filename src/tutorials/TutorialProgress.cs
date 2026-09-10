namespace GrimSpace.Tutorials;

public sealed class TutorialProgress
{
	private readonly HashSet<string> _completed = new(StringComparer.Ordinal);

	public IReadOnlySet<string> Completed => _completed;

	public bool IsCompleted(string tutorialId)
	{
		ArgumentException.ThrowIfNullOrEmpty(tutorialId);
		return _completed.Contains(tutorialId);
	}

	public void Complete(string tutorialId)
	{
		ArgumentException.ThrowIfNullOrEmpty(tutorialId);
		_completed.Add(tutorialId);
	}
}

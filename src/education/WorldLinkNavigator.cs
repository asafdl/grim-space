namespace GrimSpace.Education;

public abstract record WorldLinkNavigationResult
{
	public sealed record Followed : WorldLinkNavigationResult;

	public sealed record FocusFailed(WorldFocusResult Result) : WorldLinkNavigationResult;

	public sealed record IndicatorFailed(WorldIndicatorResult Result) : WorldLinkNavigationResult;
}

public sealed class WorldLinkNavigator(
	IWorldFocus worldFocus,
	IWorldIndicator worldIndicator) : IDisposable
{
	private readonly List<IWorldFocusHandle> _focusHandles = [];
	private IWorldIndicatorHandle? _indicatorHandle;

	public WorldLinkNavigationResult Follow(
		string objectId,
		bool focusTarget = true,
		bool showIndicator = true)
	{
		ArgumentException.ThrowIfNullOrEmpty(objectId);

		IWorldFocusHandle? nextFocusHandle = null;
		if (focusTarget)
		{
			var focus = worldFocus.Focus(objectId);
			if (focus is not WorldFocusResult.Accepted accepted)
				return new WorldLinkNavigationResult.FocusFailed(focus);
			nextFocusHandle = accepted.Handle;
		}

		if (!showIndicator)
		{
			ClearIndicator();
			if (nextFocusHandle is not null)
				_focusHandles.Add(nextFocusHandle);
			return new WorldLinkNavigationResult.Followed();
		}

		var indicator = worldIndicator.Show(objectId);
		if (indicator is not WorldIndicatorResult.Shown shown)
		{
			nextFocusHandle?.Dispose();
			return new WorldLinkNavigationResult.IndicatorFailed(indicator);
		}

		ClearIndicator();
		if (nextFocusHandle is not null)
			_focusHandles.Add(nextFocusHandle);
		_indicatorHandle = shown.Handle;
		return new WorldLinkNavigationResult.Followed();
	}

	public void ClearCurrentFocus()
	{
		if (_focusHandles.Count == 0)
			return;

		var index = _focusHandles.Count - 1;
		_focusHandles[index].Dispose();
		_focusHandles.RemoveAt(index);
	}

	public void ClearFocus()
	{
		for (var i = _focusHandles.Count - 1; i >= 0; i--)
			_focusHandles[i].Dispose();
		_focusHandles.Clear();
	}

	public void ClearIndicator()
	{
		_indicatorHandle?.Dispose();
		_indicatorHandle = null;
	}

	public void Clear()
	{
		ClearIndicator();
		ClearFocus();
	}

	public void Dispose() => Clear();
}

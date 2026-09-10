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
	private IWorldIndicatorHandle? _indicatorHandle;

	public WorldLinkNavigationResult Follow(string objectId)
	{
		ArgumentException.ThrowIfNullOrEmpty(objectId);

		var focus = worldFocus.Focus(objectId);
		if (focus is not WorldFocusResult.Accepted)
			return new WorldLinkNavigationResult.FocusFailed(focus);

		var indicator = worldIndicator.Show(objectId);
		if (indicator is not WorldIndicatorResult.Shown shown)
			return new WorldLinkNavigationResult.IndicatorFailed(indicator);

		Clear();
		_indicatorHandle = shown.Handle;
		return new WorldLinkNavigationResult.Followed();
	}

	public void Clear()
	{
		_indicatorHandle?.Dispose();
		_indicatorHandle = null;
	}

	public void Dispose() => Clear();
}

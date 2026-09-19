using GrimSpace.Education;

namespace GrimSpace.Tests.Education;

public sealed class WorldLinkNavigatorTests
{
	[Fact]
	public void Follow_HiddenFleetTarget_ReturnsFocusFailed()
	{
		var focus = new VisibilityAwareWorldFocus(_ => false);
		var indicator = new TrackingWorldIndicator();
		using var navigator = new WorldLinkNavigator(focus, indicator);

		var result = navigator.Follow("hidden-pirate");

		Assert.IsType<WorldLinkNavigationResult.FocusFailed>(result);
	}

	[Fact]
	public void Clear_OnOneNavigator_LeavesOtherNavigatorIndicatorActive()
	{
		var indicator = new TrackingWorldIndicator();
		var focus = new TrackingWorldFocus();
		using var tutorialLinks = new WorldLinkNavigator(focus, indicator);
		using var narrativeLinks = new WorldLinkNavigator(focus, indicator);

		tutorialLinks.Follow("tutorial-target");
		var tutorialHandle = indicator.ActiveHandle;

		narrativeLinks.Clear();

		Assert.False(tutorialHandle.IsDisposed);
		Assert.Equal("tutorial-target", indicator.ActiveObjectId);
	}

	private sealed class TrackingWorldFocus : IWorldFocus
	{
		public WorldFocusResult Focus(string objectId) =>
			new WorldFocusResult.Accepted(new TrackingHandle());
	}

	private sealed class VisibilityAwareWorldFocus(Func<string, bool> isVisible) : IWorldFocus
	{
		public WorldFocusResult Focus(string objectId) =>
			isVisible(objectId)
				? new WorldFocusResult.Accepted(new TrackingHandle())
				: new WorldFocusResult.TargetNotFocusable();
	}

	private sealed class TrackingWorldIndicator : IWorldIndicator
	{
		public string? ActiveObjectId { get; private set; }

		public TrackingHandle ActiveHandle { get; private set; } = null!;

		public WorldIndicatorResult Show(string objectId)
		{
			ActiveObjectId = objectId;
			ActiveHandle = new TrackingHandle();
			return new WorldIndicatorResult.Shown(ActiveHandle);
		}
	}

	private sealed class TrackingHandle : IWorldFocusHandle, IWorldIndicatorHandle
	{
		public bool IsDisposed { get; private set; }

		public void Dispose() => IsDisposed = true;
	}
}

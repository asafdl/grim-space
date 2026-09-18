using GrimSpace.Education;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Presentation;
using GrimSpace.Tests.World.StarSystem;

namespace GrimSpace.Tests.Presentation;

public sealed class MapWorldFocusTests(StarMapFixture maps)
{
	[Fact]
	public void Focus_TargetHiddenWhenDeferredCallbackRuns_DoesNotApplyFocus()
	{
		var map = maps.Template(42);
		var unitId = map.FleetRegistry.Ids.First();
		var visible = true;
		var focusApplied = false;
		Action? deferredCallback = null;
		var focus = new MapWorldFocus(
			() => map,
			_ => new Coord(1, 0, 2),
			callback =>
			{
				deferredCallback = callback;
				return true;
			},
			applyFocus =>
			{
				applyFocus();
				return new TrackingHandle();
			},
			(_, _, _) => focusApplied = true,
			_ => visible);

		var result = focus.Focus(unitId);

		Assert.IsType<WorldFocusResult.Accepted>(result);
		visible = false;
		deferredCallback!.Invoke();
		Assert.False(focusApplied);
	}

	[Fact]
	public void Focus_TargetStillVisibleWhenDeferredCallbackRuns_AppliesFocus()
	{
		var map = maps.Template(42);
		var unitId = map.FleetRegistry.Ids.First();
		var focusApplied = false;
		Coord? focusedCoord = null;
		Action? deferredCallback = null;
		var focus = new MapWorldFocus(
			() => map,
			_ => new Coord(9, 0, 8),
			callback =>
			{
				deferredCallback = callback;
				return true;
			},
			applyFocus =>
			{
				applyFocus();
				return new TrackingHandle();
			},
			(coord, _, _) =>
			{
				focusApplied = true;
				focusedCoord = coord;
			},
			_ => true);

		var result = focus.Focus(unitId);

		Assert.IsType<WorldFocusResult.Accepted>(result);
		deferredCallback!.Invoke();
		Assert.True(focusApplied);
		Assert.Equal(new Coord(9, 0, 8), focusedCoord);
	}

	[Fact]
	public void Focus_HiddenFleetAtRequest_ReturnsTargetNotFocusable()
	{
		var map = maps.Template(42);
		var unitId = map.FleetRegistry.Ids.First();
		var focus = new MapWorldFocus(
			() => map,
			_ => new Coord(1, 0, 2),
			_ => true,
			_ => new TrackingHandle(),
			(_, _, _) => { },
			_ => false);

		var result = focus.Focus(unitId);

		Assert.IsType<WorldFocusResult.TargetNotFocusable>(result);
	}

	[Fact]
	public void Focus_AcceptedHandle_IsDisposeSafeWhenFocusNeverApplied()
	{
		var map = maps.Template(42);
		var unitId = map.FleetRegistry.Ids.First();
		var visible = true;
		Action? deferredCallback = null;
		var focus = new MapWorldFocus(
			() => map,
			_ => new Coord(1, 0, 2),
			callback =>
			{
				deferredCallback = callback;
				return true;
			},
			_ => new TrackingHandle(),
			(_, _, _) => { },
			_ => visible);

		var result = (WorldFocusResult.Accepted)focus.Focus(unitId);
		visible = false;
		deferredCallback!.Invoke();

		var exception = Record.Exception(() => result.Handle.Dispose());
		Assert.Null(exception);
	}

	private sealed class TrackingHandle : IWorldFocusHandle
	{
		public void Dispose() { }
	}
}

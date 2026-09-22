using GrimSpace.Education;
using GrimSpace.World.StarSystem.Presentation.Picking;
using GrimSpace.Math.Grid;

namespace GrimSpace.World.StarSystem.Presentation.Camera;

public sealed class MapWorldFocus : IWorldFocus
{
	private readonly Func<StarMap> _world;
	private readonly Func<string, Coord> _committedPositionOf;
	private readonly Func<Action, bool> _prepareFocus;
	private readonly Func<Action, IWorldFocusHandle> _beginFocusLease;
	private readonly Action<Coord, int, int> _focusAtCoord;
	private readonly Func<string, bool>? _isFleetVisible;

	public MapWorldFocus(
		Func<StarMap> world,
		Func<string, Coord> committedPositionOf,
		Func<Action, bool> prepareFocus,
		Func<Action, IWorldFocusHandle> beginFocusLease,
		Action<Coord, int, int> focusAtCoord,
		Func<string, bool>? isFleetVisible = null)
	{
		_world = world;
		_committedPositionOf = committedPositionOf;
		_prepareFocus = prepareFocus;
		_beginFocusLease = beginFocusLease;
		_focusAtCoord = focusAtCoord;
		_isFleetVisible = isFleetVisible;
	}

	public MapWorldFocus(
		MapCamera camera,
		Func<StarMap> world,
		Func<string, Coord> committedPositionOf,
		Func<Action, bool> prepareFocus,
		Func<string, bool>? isFleetVisible = null)
		: this(
			world,
			committedPositionOf,
			prepareFocus,
			applyFocus => camera.BeginFocusLease(applyFocus),
			(coord, width, height) => camera.FocusPivot(MapMapping.ToWorld(coord, width, height)),
			isFleetVisible)
	{
	}

	public WorldFocusResult Focus(string objectId)
	{
		var world = _world();
		return WorldObjectQueries.ResolveFocusable(
			world,
			objectId,
			_committedPositionOf,
			_isFleetVisible) switch
		{
			WorldObjectResolution.Found => PrepareFocus(objectId),
			WorldObjectResolution.Missing => new WorldFocusResult.MissingTarget(),
			WorldObjectResolution.Ambiguous => new WorldFocusResult.AmbiguousTargetId(),
			WorldObjectResolution.NotFocusable => new WorldFocusResult.TargetNotFocusable(),
			_ => throw new InvalidOperationException("Unknown world object resolution."),
		};
	}

	private WorldFocusResult PrepareFocus(string objectId)
	{
		var pending = new PendingWorldFocusHandle();
		if (!_prepareFocus(() =>
			{
				var liveWorld = _world();
				var resolution = WorldObjectQueries.ResolveFocusable(
					liveWorld,
					objectId,
					_committedPositionOf,
					_isFleetVisible);
				if (resolution is not WorldObjectResolution.Found found)
				{
					pending.Cancel();
					return;
				}

				var lease = _beginFocusLease(() =>
					_focusAtCoord(found.Position, liveWorld.Width, liveWorld.Height));
				pending.AttachLease(lease);
			}))
		{
			pending.Dispose();
			return new WorldFocusResult.Unavailable();
		}

		return new WorldFocusResult.Accepted(pending);
	}
}

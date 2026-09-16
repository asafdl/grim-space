using GrimSpace.Education;
using GrimSpace.Math.Grid;

namespace GrimSpace.World.StarSystem.Presentation;

public sealed class MapWorldFocus : IWorldFocus
{
	private readonly MapCamera _camera;
	private readonly Func<StarMap> _world;
	private readonly Func<string, Coord> _committedPositionOf;
	private readonly Func<Action, bool> _prepareFocus;

	public MapWorldFocus(
		MapCamera camera,
		Func<StarMap> world,
		Func<string, Coord> committedPositionOf,
		Func<Action, bool> prepareFocus)
	{
		_camera = camera;
		_world = world;
		_committedPositionOf = committedPositionOf;
		_prepareFocus = prepareFocus;
	}

	public WorldFocusResult Focus(string objectId)
	{
		var world = _world();
		return WorldObjectQueries.ResolveFocusable(world, objectId, _committedPositionOf) switch
		{
			WorldObjectResolution.Found found => PrepareFocus(world, found),
			WorldObjectResolution.Missing => new WorldFocusResult.MissingTarget(),
			WorldObjectResolution.Ambiguous => new WorldFocusResult.AmbiguousTargetId(),
			WorldObjectResolution.NotFocusable => new WorldFocusResult.TargetNotFocusable(),
			_ => throw new InvalidOperationException("Unknown world object resolution."),
		};
	}

	private WorldFocusResult PrepareFocus(StarMap world, WorldObjectResolution.Found found)
	{
		IWorldFocusHandle? handle = null;
		if (_prepareFocus(() =>
			{
				handle = _camera.BeginFocusLease(() =>
					_camera.FocusPivot(
						MapMapping.ToWorld(found.Position, world.Width, world.Height)));
			}))
			return new WorldFocusResult.Accepted(handle!);

		handle?.Dispose();
		return new WorldFocusResult.Unavailable();
	}
}

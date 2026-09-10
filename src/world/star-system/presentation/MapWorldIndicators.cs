using Godot;
using GrimSpace.Education;
using GrimSpace.Math.Grid;

namespace GrimSpace.World.StarSystem.Presentation;

public sealed partial class MapWorldIndicators : Node3D, IWorldIndicator
{
	private readonly Dictionary<long, Indicator> _indicators = [];
	private Func<StarMap> _world = null!;
	private Func<string, Coord> _committedPositionOf = null!;
	private Func<bool> _isAvailable = null!;
	private Func<Node3D> _createVisual = null!;
	private long _nextId;
	private bool _configured;

	public void Configure(
		Func<StarMap> world,
		Func<string, Coord> committedPositionOf,
		Func<bool> isAvailable,
		Func<Node3D> createVisual)
	{
		_world = world;
		_committedPositionOf = committedPositionOf;
		_isAvailable = isAvailable;
		_createVisual = createVisual;
		_configured = true;
	}

	public WorldIndicatorResult Show(string objectId)
	{
		EnsureConfigured();
		if (!_isAvailable())
			return new WorldIndicatorResult.Unavailable();

		var world = _world();
		return WorldObjectQueries.ResolveFocusable(world, objectId, _committedPositionOf) switch
		{
			WorldObjectResolution.Found found => Show(world, objectId, found),
			WorldObjectResolution.Missing => new WorldIndicatorResult.MissingTarget(),
			WorldObjectResolution.Ambiguous => new WorldIndicatorResult.AmbiguousTargetId(),
			WorldObjectResolution.NotFocusable => new WorldIndicatorResult.TargetNotFocusable(),
			_ => throw new InvalidOperationException("Unknown world object resolution."),
		};
	}

	public override void _Process(double delta)
	{
		if (!_configured)
			return;

		if (!_isAvailable())
		{
			foreach (var indicator in _indicators.Values)
				indicator.Root.Visible = false;
			return;
		}

		var world = _world();
		foreach (var (id, indicator) in _indicators.ToArray())
		{
			var resolution = WorldObjectQueries.ResolveFocusable(
				world,
				indicator.ObjectId,
				_committedPositionOf);
			if (resolution is WorldObjectResolution.Found found)
			{
				indicator.Root.Visible = true;
				indicator.Root.Position = MapMapping.ToWorld(
					found.Position,
					world.Width,
					world.Height);
				continue;
			}

			GD.PushWarning(
				$"World indicator target '{indicator.ObjectId}' became unavailable: {resolution.GetType().Name}.");
			Remove(id);
		}
	}

	private WorldIndicatorResult Show(
		StarMap world,
		string objectId,
		WorldObjectResolution.Found found)
	{
		var root = new Node3D
		{
			Name = $"WorldIndicator_{_nextId}",
			Position = MapMapping.ToWorld(found.Position, world.Width, world.Height),
		};
		root.AddChild(_createVisual());
		AddChild(root);

		var id = _nextId++;
		_indicators.Add(id, new Indicator(objectId, root));
		return new WorldIndicatorResult.Shown(new IndicatorHandle(this, id));
	}

	private void Remove(long id)
	{
		if (!_indicators.Remove(id, out var indicator))
			return;

		indicator.Root.QueueFree();
	}

	private void EnsureConfigured()
	{
		if (!_configured)
			throw new InvalidOperationException("World indicators must be configured before use.");
	}

	private sealed record Indicator(string ObjectId, Node3D Root);

	private sealed class IndicatorHandle(MapWorldIndicators owner, long id) : IWorldIndicatorHandle
	{
		private MapWorldIndicators? _owner = owner;

		public void Dispose()
		{
			if (_owner is null)
				return;

			if (GodotObject.IsInstanceValid(_owner))
				_owner.Remove(id);
			_owner = null;
		}
	}
}

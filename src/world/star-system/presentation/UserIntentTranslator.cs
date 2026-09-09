using Godot;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Agents;

namespace GrimSpace.World.StarSystem.Presentation;

public sealed class UserIntentTranslator
{
	private readonly StarMapPlayerExecutionAgent _playerAgent;
	private readonly MapCamera _camera;
	private readonly Func<Vector2> _screenPosition;
	private readonly Func<int> _mapWidth;
	private readonly Func<int> _mapHeight;
	private readonly Func<Coord, Coord>? _resolveDestination;
	private readonly Func<Coord, string?>? _unitAt;
	private Vector2? _rmbPressPosition;

	public UserIntentTranslator(
		StarMapPlayerExecutionAgent playerAgent,
		MapCamera camera,
		Func<Vector2> screenPosition,
		Func<int> mapWidth,
		Func<int> mapHeight,
		Func<Coord, Coord>? resolveDestination = null,
		Func<Coord, string?>? unitAt = null)
	{
		_playerAgent = playerAgent;
		_camera = camera;
		_screenPosition = screenPosition;
		_mapWidth = mapWidth;
		_mapHeight = mapHeight;
		_resolveDestination = resolveDestination;
		_unitAt = unitAt;
	}

	public bool TryHandleMouseButton(InputEventMouseButton mouseButton, out bool unreachable)
	{
		unreachable = false;
		if (mouseButton.ButtonIndex == MouseButton.Right && mouseButton.Pressed)
		{
			_rmbPressPosition = mouseButton.Position;
			return true;
		}

		if (mouseButton.ButtonIndex != MouseButton.Right
			|| mouseButton.Pressed
			|| _rmbPressPosition is not { } pressPosition
			|| pressPosition.DistanceTo(mouseButton.Position) >= 4f)
		{
			_rmbPressPosition = null;
			return false;
		}

		_rmbPressPosition = null;
		var result = TryQueueIntent();
		unreachable = result is CourseCommandResult.Unreachable;
		return result is not CourseCommandResult.Ignored;
	}

	public CourseCommandResult TryQueueIntent()
	{
		var destination = MapPick.PickPoint(_camera, _screenPosition(), _mapWidth(), _mapHeight());
		if (destination is null)
		{
			StarMapPresentationDiagnostics.LogMovePickMiss();
			return new CourseCommandResult.Ignored();
		}

		if (_unitAt?.Invoke(destination.Value) is { } targetUnitId)
			return _playerAgent.TryQueueHuntUnit(targetUnitId);

		var resolved = _resolveDestination?.Invoke(destination.Value) ?? destination.Value;
		return _playerAgent.TryQueueMove(resolved);
	}
}

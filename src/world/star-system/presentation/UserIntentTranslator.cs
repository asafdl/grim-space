using Godot;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Agents;

namespace GrimSpace.World.StarSystem.Presentation;

public sealed class UserIntentTranslator
{
	private readonly StarMapPlayerExecutionAgent _playerAgent;
	private readonly MapCamera _camera;
	private readonly Func<Vector2> _screenPosition;
	private readonly Func<int> _mapWidth;
	private readonly Func<int> _mapHeight;
	private Vector2? _rmbPressPosition;

	public UserIntentTranslator(
		StarMapPlayerExecutionAgent playerAgent,
		MapCamera camera,
		Func<Vector2> screenPosition,
		Func<int> mapWidth,
		Func<int> mapHeight)
	{
		_playerAgent = playerAgent;
		_camera = camera;
		_screenPosition = screenPosition;
		_mapWidth = mapWidth;
		_mapHeight = mapHeight;
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
		var result = TryQueueMove();
		unreachable = result == MoveQueueResult.Unreachable;
		return result != MoveQueueResult.Ignored;
	}

	public MoveQueueResult TryQueueMove()
	{
		var destination = MapPick.PickPoint(_camera, _screenPosition(), _mapWidth(), _mapHeight());
		if (destination is null)
			return MoveQueueResult.Ignored;

		return _playerAgent.TryQueueMove(destination.Value) switch
		{
			MoveCommandResult.Queued => MoveQueueResult.Queued,
			MoveCommandResult.Unreachable => MoveQueueResult.Unreachable,
			_ => MoveQueueResult.Ignored,
		};
	}

	public enum MoveQueueResult
	{
		Ignored,
		Queued,
		Unreachable,
	}
}

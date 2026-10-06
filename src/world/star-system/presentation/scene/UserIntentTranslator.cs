using Godot;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Agents;
using GrimSpace.World.StarSystem.Presentation.Camera;
using GrimSpace.World.StarSystem.Presentation.Diagnostics;
using GrimSpace.World.StarSystem.Presentation.Picking;

namespace GrimSpace.World.StarSystem.Presentation.Scene;

public sealed class UserIntentTranslator
{
	private readonly StarMapPlayerExecutionAgent _playerAgent;
	private readonly Func<Coord, Coord>? _resolveDestination;
	private readonly Func<MapInteractiveTarget> _resolveTarget;
	private Vector2? _lmbPressPosition;
	private MapInteractiveTarget? _lmbPressUnit;

	public UserIntentTranslator(
		StarMapPlayerExecutionAgent playerAgent,
		Func<Coord, Coord>? resolveDestination,
		Func<MapInteractiveTarget> resolveTarget)
	{
		_playerAgent = playerAgent;
		_resolveDestination = resolveDestination;
		_resolveTarget = resolveTarget;
	}

	public bool TryHandleMouseButton(InputEventMouseButton mouseButton, out bool unreachable)
	{
		unreachable = false;
		if (mouseButton.ButtonIndex == MouseButton.Left && mouseButton.Pressed)
		{
			_lmbPressPosition = mouseButton.Position;
			var pressTarget = _resolveTarget();
			_lmbPressUnit = pressTarget.Kind == MapInteractiveTargetKind.Unit ? pressTarget : null;
			return true;
		}

		if (mouseButton.ButtonIndex != MouseButton.Left
			|| mouseButton.Pressed
			|| _lmbPressPosition is not { } pressPosition
			|| pressPosition.DistanceTo(mouseButton.Position) >= 4f)
		{
			_lmbPressPosition = null;
			_lmbPressUnit = null;
			return false;
		}

		_lmbPressPosition = null;
		var target = _lmbPressUnit ?? _resolveTarget();
		_lmbPressUnit = null;
		var result = TryQueueIntent(target);
		unreachable = result is CourseCommandResult.Unreachable;
		return result is not CourseCommandResult.Ignored;
	}

	public CourseCommandResult TryQueueIntent(MapInteractiveTarget target) =>
		target.Kind switch
		{
			MapInteractiveTargetKind.Unit when target.Unit is { } unit =>
				_playerAgent.TryQueuePursueFleet(unit.UnitId),
			MapInteractiveTargetKind.Wreck when target.WreckContractId is { } wreckContractId =>
				_playerAgent.TryQueueWreckContact(wreckContractId),
			MapInteractiveTargetKind.SelfClickNoOp =>
				new CourseCommandResult.SelfClickIgnored(),
			MapInteractiveTargetKind.None
				or MapInteractiveTargetKind.Landmark
				or MapInteractiveTargetKind.Dock
				or MapInteractiveTargetKind.Poi when target.MoveGridPoint is { } pick =>
				_playerAgent.TryQueueMove(_resolveDestination?.Invoke(pick) ?? pick),
			_ => LogMovePickMissAndIgnore(),
		};

	private CourseCommandResult LogMovePickMissAndIgnore()
	{
		StarMapPresentationDiagnostics.LogMovePickMiss();
		return new CourseCommandResult.Ignored();
	}
}

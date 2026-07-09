using System.Collections.Generic;
using Godot;
using GrimSpace.Battle;

namespace GrimSpace.Battle;

public partial class BattleController : Node3D
{
	private const int GridSize = 8;

	private BattleState _battle = null!;
	private GridView _gridView = null!;
	private BattleCameraController _camera = null!;
	private Label _hintLabel = null!;

	private UnitView _playerView = null!;
	private UnitView _enemyView = null!;

	private readonly List<GridCoord> _aimCandidates = new();
	private int _aimIndex;
	private Vector2 _lastAimScreenPos;

	public override void _Ready()
	{
		_camera = GetNode<BattleCameraController>("Camera3D");
		_gridView = GetNode<GridView>("GridView");

		_battle = new BattleState(
			GridSize,
			GridSize,
			GridSize,
			new GridCoord(1, 1, 1),
			new GridCoord(6, 6, 6));

		_gridView.Build(_battle.Grid);
		SetupHintLabel();
		UpdateMoveHighlights();

		var unitsRoot = GetNode<Node3D>("Units");
		_playerView = new UnitView();
		_playerView.Bind(_battle.Player);
		unitsRoot.AddChild(_playerView);

		_enemyView = new UnitView();
		_enemyView.Bind(_battle.Enemy);
		unitsRoot.AddChild(_enemyView);

		UpdateAimCandidates(GetViewport().GetVisibleRect().GetCenter());
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseButton { Pressed: true } mouseButton)
		{
			switch (mouseButton.ButtonIndex)
			{
				case MouseButton.WheelUp:
					CycleAim(-1);
					GetViewport().SetInputAsHandled();
					break;
				case MouseButton.WheelDown:
					CycleAim(1);
					GetViewport().SetInputAsHandled();
					break;
			}
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
		{
			var target = GetAimTarget();
			if (target is null)
				return;

			if (!_battle.TryMove(_battle.Player, target.Value))
				return;

			_playerView.SyncPosition();
			UpdateAimCandidates(GetViewport().GetMousePosition());
			UpdateMoveHighlights();
			return;
		}

		if (@event is InputEventMouseMotion motion)
			UpdateAimCandidates(motion.Position);
	}

	private void SetupHintLabel()
	{
		var canvas = new CanvasLayer();
		_hintLabel = new Label
		{
			Position = new Vector2(16, 16),
		};
		canvas.AddChild(_hintLabel);
		AddChild(canvas);
		UpdateHintLabel();
	}

	private void UpdateHintLabel()
	{
		var target = GetAimTarget();
		var targetText = target is null ? "—" : $"({target.Value.X}, {target.Value.Y}, {target.Value.Z})";
		var indexText = _aimCandidates.Count == 0 ? "0/0" : $"{_aimIndex + 1}/{_aimCandidates.Count}";
		_hintLabel.Text = $"Aim: {targetText}  [{indexText}]  |  Scroll: cycle depth  |  +/-: zoom  |  RMB: orbit";
	}

	private void UpdateAimCandidates(Vector2 screenPos)
	{
		if (screenPos.DistanceTo(_lastAimScreenPos) > 2f)
			_aimIndex = 0;

		_lastAimScreenPos = screenPos;

		var origin = _camera.ProjectRayOrigin(screenPos);
		var direction = _camera.ProjectRayNormal(screenPos);

		_aimCandidates.Clear();
		_aimCandidates.AddRange(GridRayMarch.March(
			_battle.Grid,
			origin.X, origin.Y, origin.Z,
			direction.X, direction.Y, direction.Z,
			GridView.CellSize));

		if (_aimCandidates.Count == 0)
			_aimIndex = 0;
		else
			_aimIndex = Mathf.Clamp(_aimIndex, 0, _aimCandidates.Count - 1);

		UpdateMoveHighlights();
	}

	private void CycleAim(int delta)
	{
		if (_aimCandidates.Count == 0)
			return;

		_aimIndex = (_aimIndex + delta) % _aimCandidates.Count;
		if (_aimIndex < 0)
			_aimIndex += _aimCandidates.Count;

		UpdateMoveHighlights();
	}

	private GridCoord? GetAimTarget() =>
		_aimCandidates.Count == 0 ? null : _aimCandidates[_aimIndex];

	private void UpdateMoveHighlights()
	{
		var reachable = new List<GridCoord>();

		for (var x = 0; x < _battle.Grid.Width; x++)
		{
			for (var y = 0; y < _battle.Grid.Height; y++)
			{
				for (var z = 0; z < _battle.Grid.Depth; z++)
				{
					var coord = new GridCoord(x, y, z);
					if (_battle.CanMoveTo(_battle.Player, coord))
						reachable.Add(coord);
				}
			}
		}

		_gridView.SetCellHighlights(reachable, _aimCandidates, GetAimTarget());
		UpdateHintLabel();
	}
}

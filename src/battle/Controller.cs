using Godot;
using GrimSpace.Battle.Camera;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Units;
using GrimSpace.Core;
using GrimSpace.Domain.Units.Enums;
using ClickResult = GrimSpace.Battle.Presentation.ClickResult;
using GridView = GrimSpace.Battle.Grid.View;
using MoveUi = GrimSpace.Battle.Presentation.Movement;

namespace GrimSpace.Battle;

public partial class Controller : Node3D
{
	private Manager _manager = null!;

	private GridView _gridView = null!;
	private Camera.Controller _camera = null!;
	private Label _hintLabel = null!;

	private readonly Selection _selection = new();
	private readonly Dictionary<string, Units.View> _unitViews = new();

	private Unit? _previewUnit;
	private IReadOnlyList<Option> _options = [];

	public override void _Ready()
	{
		_manager = Manager.FromEncounter(Session.Instance.CurrentEncounter);
		_camera = GetNode<Camera.Controller>("Camera3D");
		_gridView = GetNode<GridView>("GridView");
		_gridView.Build(_manager.Grid);

		var unitsRoot = GetNode<Node3D>("Units");
		foreach (var unit in _manager.Units)
		{
			var view = new Units.View();
			view.Bind(unit.State, ColorFor(unit.Controller));
			unitsRoot.AddChild(view);
			_unitViews[unit.State.Id] = view;
		}

		SetupHintLabel();
		Refresh();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey { Pressed: true, Keycode: Key.Space })
		{
			if (_previewUnit is not null && _selection.SelectedIndex is int selected)
				_manager.CommitMove(_previewUnit, _options[selected]);

			_manager.RequestEndTurn();

			foreach (var unit in _manager.Units)
				_unitViews[unit.State.Id].SyncPosition();

			_selection.Clear();
			Refresh();
			GetViewport().SetInputAsHandled();
			return;
		}

		if (_previewUnit is null)
			return;

		if (@event is InputEventMouseMotion motion)
		{
			var hovered = MoveUi.PickOptionIndex(_camera, motion.Position, _options);
			_selection.SetHover(hovered, _options.Count);
			Refresh();
			return;
		}

		if (@event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } click)
			return;

		var picked = MoveUi.PickOptionIndex(_camera, click.Position, _options);
		if (_selection.OnClick(picked, _options.Count) != ClickResult.Confirm)
		{
			Refresh();
			return;
		}

		if (_selection.SelectedIndex is not int index)
			return;

		if (_manager.CommitMove(_previewUnit, _options[index]))
			_selection.Clear();

		Refresh();
	}

	private void Refresh()
	{
		var activePreview = _manager.ShowMovementForActiveUnits().FirstOrDefault();
		_previewUnit = activePreview.Unit;
		_options = activePreview.Preview?.Options ?? [];

		_selection.ClampToCount(_options.Count);

		var (path, target) = MoveUi.GetHighlights(
			_options, _selection.SelectedIndex, _selection.HoveredIndex);
		_gridView.SetHighlights(_options, path, target);

		var turnPrefix = $"Turn {_manager.Turn.TurnNumber}  |  Space: end turn  |  ";
		_hintLabel.Text = turnPrefix + (_previewUnit is null
			? "No movement preview  |  scroll/+/-: zoom  |  RMB: orbit"
			: MoveUi.BuildHint(
				_options,
				_selection.SelectedIndex,
				_selection.HoveredIndex,
				_previewUnit.State.ActionPoints));
	}

	private static Color ColorFor(EController controller) =>
		controller switch
		{
			EController.Player => new Color(0.25f, 0.85f, 0.35f),
			EController.Enemy => new Color(0.9f, 0.25f, 0.2f),
			_ => Colors.White,
		};

	private void SetupHintLabel()
	{
		var canvas = new CanvasLayer();
		_hintLabel = new Label { Position = new Vector2(16, 16) };
		canvas.AddChild(_hintLabel);
		AddChild(canvas);
	}
}

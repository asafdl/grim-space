using Godot;
using GrimSpace.Application;
using GrimSpace.Battle;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Objectives;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Presentation.Camera;
using GrimSpace.Battle.Presentation.Domains.Move;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.World.Factions;

namespace GrimSpace.Scenes.Dev;

public partial class MovementPickLab : Node3D
{
	private static readonly Color PlayerColor = new(0.32f, 0.78f, 0.56f);
	private static readonly Color EnemyColor = new(0.9f, 0.25f, 0.3f);
	private static readonly Coord EnemyPosition = new(19, 17, 22);

	private PresentationFrameBuilder _frames = new();
	private BattleOrchestrator _battle = null!;
	private Controller _camera = null!;
	private BattleCameraDirector _cameraDirector = null!;
	private BattleView _units = null!;
	private Button _moveButton = null!;
	private RelationshipGuideView _relationshipGuide = null!;
	private GridView _grid = null!;
	private MoveGhostView _ghost = null!;
	private CellVolumeMeshStore _meshes = null!;
	private PresentationFrame _frame = null!;
	private Label _readout = null!;
	private string _message = "Hover to preview; click to pin.";
	private MoveRouteRibbonView _routeRibbon = null!;
	private bool _showRouteRibbon = true;
	private bool _ctrlHeld;
	private MoveCandidateOutlineView _candidateHighlights = null!;
	private Label _candidateReadout = null!;

	public override void _Ready()
	{
		CreateEncounter(new Coord(16, 16, 16), Coord.Forward, Coord.Up);

		AddChild(new WorldEnvironment
		{
			Environment = new Godot.Environment
			{
				BackgroundMode = Godot.Environment.BGMode.Color,
				BackgroundColor = new Color(0.008f, 0.012f, 0.022f),
				AmbientLightSource = Godot.Environment.AmbientSource.Color,
				AmbientLightColor = new Color(0.65f, 0.72f, 0.85f),
				AmbientLightEnergy = 0.8f,
			},
		});
		AddChild(new DirectionalLight3D
		{
			RotationDegrees = new Vector3(-48, -28, 0),
			LightEnergy = 1.8f,
		});
		_camera = new Controller
		{
			Name = "Camera3D", Current = true,
			HeldGestureModifierAction = "battle_isolate_movement",
		};
		AddChild(_camera);
		_cameraDirector = new BattleCameraDirector(_camera);
		_camera.ManualInputStarted += _cameraDirector.OnManualInputStarted;
		ResetCamera();

		_meshes = new CellVolumeMeshStore();
		_grid = new GridView { Name = "MovementGrid" };
		AddChild(_grid);
		_grid.Build(_camera, _meshes);
		_ghost = new MoveGhostView { Name = "MoveGhost", ShowAxisGuides = false };
		_ghost.Configure(_camera);
		AddChild(_ghost);
		_ghost.RollButton.GuiInput += @event =>
		{
			if (@event is not InputEventMouseButton { Pressed: true } click
				|| !click.IsActionPressed("battle_primary_click", false, true)
				|| _frame.SelectedMove is not { } selected)
				return;

			var bases = _frame.MovePaths
				.Where(path => path.EndPosition == selected.EndPosition)
				.Select(path => path.EndBasis).ToArray();
			if (MovePose.CycleRoll(bases, selected.EndBasis, 1) is { } basis
				&& basis != selected.EndBasis)
			{
				_frames.Interaction.SetMovePose(basis);
				RefreshFrame();
			}
			_ghost.RollButton.AcceptEvent();
		};

		_units = new BattleView { Name = "Ships" };
		AddChild(_units);
		_relationshipGuide = new RelationshipGuideView
		{
			Name = "RelationshipGuide",
			ShowReticle = false,
			AnimateAlignment = true,
		};
		AddChild(_relationshipGuide);
		_routeRibbon = new MoveRouteRibbonView { Name = "MoveRouteRibbon" };
		AddChild(_routeRibbon);
		_candidateHighlights = new MoveCandidateOutlineView { Name = "LockedCandidateCells" };
		AddChild(_candidateHighlights);
		BuildHud();
		RebuildLab();
	}

	public override void _Process(double delta)
	{
		UpdateCandidateLock(Input.IsActionPressed("battle_isolate_movement") && GetWindow().HasFocus());
		if (_cameraDirector.NeedsTick)
			_cameraDirector.Tick((float)delta, WorldMapping.ToWorld(_frame.FocusState.Position));
		var updates = _meshes.Pump(_frame.SimulationTick);
		foreach (var failure in updates.Failures)
			GD.PushError($"Movement pick lab mesh generation failed for {failure.Key}: {failure.Error}");
		if (updates.ProcessedCount > 0)
			_grid.ApplyFrame(_frame);

		if (_frames.Interaction.MoveDestination is not null)
			return;

		var canPick = CanPick();
		var mouse = GetViewport().GetMousePosition();
		var previous = _frames.Interaction.MoveHoveredCell;
		var hovered = canPick
			? MovementSelection.PickCoordinate(
				_camera, GetViewport(), mouse,
				_frame.MovePaths, previous, _frame.MovementPickPreference,
				_frames.Interaction.IsolatedMoveCells)
			: null;
		if (hovered == _frames.Interaction.MoveHoveredCell)
			return;

		_frames.Interaction.SetMoveHover(hovered, _frame.MovePaths);
		RefreshFrame();
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsAction("battle_isolate_movement"))
		{
			UpdateCandidateLock(Input.IsActionPressed("battle_isolate_movement") && GetWindow().HasFocus());
			GetViewport().SetInputAsHandled();
		}
	}

	private void UpdateCandidateLock(bool held)
	{
		if (held == _ctrlHeld)
			return;
		_ctrlHeld = held;
		if (!held)
			_frames.Interaction.ReleaseMoveIsolation();
		else if (CanPick() && _frame.SelectedMove is null)
			_frames.Interaction.IsolateMoveCells(MovementSelection.BuildCandidates(
				_camera, GetViewport(), GetViewport().GetMousePosition(), _frame.MovePaths));
		RefreshFrame();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (_frames.Interaction.IsolatedMoveCells is not null)
			@event = GameInputBindings.WithoutHeldActionModifiers(@event, "battle_isolate_movement");
		if (@event is InputEventKey { Pressed: true, Echo: false } key)
		{
			if (key.Keycode == Key.Escape)
				ClearSelection();
			else if (key.Keycode == Key.R)
				ResetCamera();
			else if (key.Keycode == Key.Enter || key.Keycode == Key.KpEnter)
				MoveHere();
			else if (key.IsActionPressed("battle_focus", false, true))
				_cameraDirector.FocusPlayer(WorldMapping.ToWorld(_frame.FocusState.Position));
			else
				return;
		}
		else if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } click
			&& CanPick())
		{
			if (_frame.SelectedMove is { } selected)
			{
				var heading = MovementSelection.PickHeadingHandle(
					_camera, click.Position, selected.EndPosition, _frame.ReachableMoveHeadings);
				var bases = _frame.MovePaths
					.Where(path => path.EndPosition == selected.EndPosition)
					.Select(path => path.EndBasis).ToArray();
				if (heading is Coord direction
					&& MovePose.SelectHeading(bases, selected.EndBasis, direction) is { } basis)
					_frames.Interaction.SetMovePose(basis);
			}
			else
			{
				// Pin the displayed preview, matching battle's click/hover consistency.
				var option = _frame.HoveredMove;
				if (option is not null)
				{
					_frames.Interaction.BeginMoveSelection(option.EndPosition, option.EndBasis);
					_message = "Pinned. Choose heading, then Enter.";
				}
				else
					_message = "Hover a reachable destination first.";
			}
			RefreshFrame();
		}
		else
			return;

		GetViewport().SetInputAsHandled();
	}

	public override void _ExitTree()
	{
		if (_camera is not null)
			_camera.ManualInputStarted -= _cameraDirector.OnManualInputStarted;
		_meshes?.Dispose();
		_battle?.Dispose();
	}

	private bool CanPick() =>
		!_camera.IsManualGestureActive
		&& GetViewport().GuiGetHoveredControl() is null;

	private void ClearSelection()
	{
		_frames.Interaction.ClearMoveSelection();
		_frames.Interaction.ClearHovers();
		_message = "Hover to preview; click to pin.";
		RefreshFrame();
	}

	private void ResetCamera()
	{
		var pose = BattleCameraPoses.PlayerRearQuarter(_battle.PlayerAgent.Sim.World.StateOf(_battle.PlayerId));
		_camera.SetFocus(pose.Pivot, pose.Distance, pose.Yaw, pose.Pitch);
	}

	private void CreateEncounter(Coord playerPosition, Coord fore, Coord dorsal)
	{
		_battle?.Dispose();
		_battle = BattleOrchestrator.FromEncounter(new BattleEncounter
		{
			Id = "movement-pick-lab",
			Seed = 42,
			Objective = EObjective.EliminateOpponents,
			Spawns =
			[
				new BattleSpawn
				{
					Ship = ShipInstance.FromCatalog("lab-player", EType.Fighter),
					Team = ETeam.Player,
			Faction = EFaction.Player,
					Position = playerPosition,
					Fore = fore,
					Dorsal = dorsal,
					ExecutionAgent = new UserExecutionAgent(),
				},
				new BattleSpawn
				{
					Ship = ShipInstance.FromCatalog("lab-enemy", EType.IndustrialGooper),
					Team = ETeam.Enemy,
			Faction = EFaction.TheOptimality,
					Position = EnemyPosition,
					Fore = -Coord.Forward,
					ExecutionAgent = new AiController(),
				},
			],
		}, gridSize: 32);
		_battle.GrantPlayerCanWork();
	}

	private void RebuildLab()
	{
		UpdateCandidateLock(false);
		_frames = new PresentationFrameBuilder();
		_frame = _frames.BuildFrame(_battle, _battle.PlayerAgent, acceptsCommands: true);
		_units.ApplyUnitStates(_frame.PreviewUnits.ToDictionary(pair => pair.Key, pair => pair.Value.ToState()),
			id => _frame.PreviewUnitTeams[id] == ETeam.Player ? PlayerColor : EnemyColor);
		ApplyFrame();
	}

	public void MoveHere()
	{
		if (_frame.SelectedMove is not { } selected)
		{
			_message = "Select a destination before moving.";
			ApplyFrame();
			return;
		}
		if (!_battle.PlayerAgent.TryEnqueue(selected.Steps))
		{
			GD.PushError("Movement pick lab could not enqueue the selected movement path.");
			_message = "Move rejected. Esc to clear and retry.";
			ApplyFrame();
			return;
		}
		var result = _battle.PlayerAgent.Sim.World.StateOf(_battle.PlayerId);
		var position = result.Position;
		var fore = result.Fore;
		var dorsal = result.Dorsal;
		// Each move starts a fresh sandbox encounter, restoring resources without changing live battle rules.
		CreateEncounter(position, fore, dorsal);
		_message = "Moved. Resources refreshed.";
		RebuildLab();
		_cameraDirector.ReturnControl(WorldMapping.ToWorld(position));
	}

	public void ResetLab()
	{
		CreateEncounter(new Coord(16, 16, 16), Coord.Forward, Coord.Up);
		_message = "Lab reset. Hover to preview.";
		RebuildLab();
		ResetCamera();
	}

	private void RefreshFrame()
	{
		_frame = _frames.BuildFrame(_battle, _battle.PlayerAgent, acceptsCommands: true);
		ApplyFrame();
	}

	private void ApplyFrame()
	{
		_moveButton.Disabled = _frame.SelectedMove is null;
		_grid.ApplyFrame(_frame);
		_ghost.Apply(_frame.MoveGhostState, _frame.MoveCheckpoints, _frame.FocusState,
			_frame.ReachableMoveHeadings, PlayerColor, selected: _frame.SelectedMove is not null);
		var option = _frame.SelectedMove ?? _frame.HoveredMove;
		_candidateHighlights.Apply(_frame.IsolatedMoveCells, enabled: true);
		_candidateReadout.Text = _frame.IsolatedMoveCells is not { } cells
			? $"Hold {GameInputBindings.Label("battle_isolate_movement")}: isolate choices, then orbit."
			: cells.Count == 0
				? "No choices locked. Release and retry."
				: $"{cells.Count} cells locked. Orbit to separate.";
		_routeRibbon.ApplyFrame(_frame, _showRouteRibbon);
		ApplyRelationshipGuide();
		_readout.Text =
			$"Destination: {Format(option?.EndPosition)}{(_frame.SelectedMove is not null ? " (pinned)" : "")}\n"
			+ (option is null
				? ""
				: $"AP: {option.ExtensionApCost} | MP: {option.ExtensionMpCost}\n")
			+ _message;
	}

	private void ApplyRelationshipGuide()
	{
		if (_frame.RangeEnemy is not { } target
			|| !_frame.PreviewUnits.TryGetValue(target.EnemyId, out var enemy))
		{
			_relationshipGuide.Visible = false;
			return;
		}

		_relationshipGuide.Visible = true;
		_relationshipGuide.Apply(
			(_frame.MoveGhostState ?? _frame.FocusState).Position,
			enemy.Position,
			RelationshipGuideView.ReferencePlane.XZ,
			showAlignment: BattleDistance.SeparationMeters(_frame.FocusState.Position, enemy.Position)
				< RelationshipGuideView.AlignmentDistanceMeters);
	}

	private void BuildHud()
	{
		var layer = new CanvasLayer { Layer = 40 };
		AddChild(layer);
		var panel = new PanelContainer { Position = new Vector2(20, 20) };
		layer.AddChild(panel);
		var margin = new MarginContainer();
		foreach (var edge in new[] { "left", "top", "right", "bottom" })
			margin.AddThemeConstantOverride($"margin_{edge}", 12);
		panel.AddChild(margin);
		var column = new VBoxContainer();
		column.AddThemeConstantOverride("separation", 8);
		margin.AddChild(column);
		column.AddChild(new Label
		{
			Text = "Movement lab\n"
				+ "Click: pin / orient | Enter: move\n"
				+ "Esc: clear | R: reset camera\n"
				+ "Movement resources refresh after moving.",
			MouseFilter = Control.MouseFilterEnum.Ignore,
		});
		_readout = new Label { MouseFilter = Control.MouseFilterEnum.Ignore };
		column.AddChild(_readout);
		_candidateReadout = new Label
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		column.AddChild(_candidateReadout);
		var route = new CheckButton
		{
			Text = "Experiment: route ribbon",
			TooltipText = "Off: current battle preview. On: add the heading-colored route ribbon.",
			ButtonPressed = _showRouteRibbon,
			FocusMode = Control.FocusModeEnum.None,
		};
		route.Toggled += enabled =>
		{
			_showRouteRibbon = enabled;
			_routeRibbon.ApplyFrame(_frame, _showRouteRibbon);
		};
		column.AddChild(route);
		var actions = new HBoxContainer();
		column.AddChild(actions);
		_moveButton = new Button
		{
			Text = "Move here (Enter)", Disabled = true, FocusMode = Control.FocusModeEnum.None,
		};
		_moveButton.Pressed += MoveHere;
		actions.AddChild(_moveButton);
		var resetLab = new Button { Text = "Reset lab", FocusMode = Control.FocusModeEnum.None };
		resetLab.Pressed += ResetLab;
		actions.AddChild(resetLab);
	}

	private static string Format(Coord? coordinate) =>
		coordinate is Coord cell ? $"({cell.X}, {cell.Y}, {cell.Z})" : "--";
}

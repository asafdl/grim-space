using Godot;
using GrimSpace.Battle;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Objectives;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Presentation.Camera;
using GrimSpace.Battle.Presentation.Domains.Move;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Battle.Presentation.Picking;
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

	private PresentationFrameBuilder _frames = new();
	private PlanningPreview _preview = new();
	private readonly Dictionary<Coord, MovementPickPreference> _preferences = [];
	private readonly Dictionary<MovePathOption, bool> _firingOpportunities = [];
	private Dictionary<Coord, MovePathOption[]> _optionsByDestination = [];
	private Func<Coord, MovementPickPreference> _destinationPreference = null!;
	private string _referenceEnemyId = null!;
	private Coord _referenceEnemyPosition;
	private bool _assistedPicking = true;
	private Coord? _lastLoggedSnap;
	private BattleOrchestrator _battle = null!;
	private Controller _camera = null!;
	private BattleCameraDirector _cameraDirector = null!;
	private BattleView _units = null!;
	private Label3D _targetMarker = null!;
	private Button _moveButton = null!;
	private bool _showCellColors = true;
	private GridView _grid = null!;
	private MoveGhostView _ghost = null!;
	private CellVolumeMeshStore _meshes = null!;
	private PresentationFrame _frame = null!;
	private Label _readout = null!;
	private readonly Dictionary<Coord, (Color Color, StandardMaterial3D Fill, StandardMaterial3D Outline)> _cellColors = [];
	private Node3D _coloredCells = null!;
	private Coord? _highlightedCell;
	private int _destinationCount;
	private string _message = "Hover to preview; click to pin a destination.";

	public override void _Ready()
	{
		CreateEncounter(new Coord(16, 16, 16), Coord.Forward, Coord.Up, new Coord(19, 17, 22));

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
		_camera = new Controller { Name = "Camera3D", Current = true };
		AddChild(_camera);
		_cameraDirector = new BattleCameraDirector(_camera);
		_camera.ManualInputStarted += _cameraDirector.OnManualInputStarted;
		ResetCamera();

		_meshes = new CellVolumeMeshStore();
		_grid = new GridView { Name = "MovementGrid" };
		AddChild(_grid);
		_grid.Build(_camera, _meshes);
		_ghost = new MoveGhostView { Name = "MoveGhost" };
		_ghost.Configure(_camera);
		AddChild(_ghost);

		_targetMarker = new Label3D
		{
			Text = "ASSIST TARGET",
			Position = WorldMapping.ToWorld(_referenceEnemyPosition) + Vector3.Up,
			Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
			Modulate = Colors.Gold,
			NoDepthTest = true,
			PixelSize = 0.01f,
			FontSize = 36,
		};
		PresentationLayers.MarkUx(_targetMarker);
		AddChild(_targetMarker);

		_units = new BattleView { Name = "Ships" };
		AddChild(_units);
		BuildHud();
		RebuildLab();
	}

	public override void _Process(double delta)
	{
		if (_cameraDirector.NeedsTick)
			_cameraDirector.Tick((float)delta, WorldMapping.ToWorld(_frame.FocusState.Position));
		var updates = _meshes.Pump(_frame.SimulationTick);
		foreach (var failure in updates.Failures)
			GD.PushError($"Movement pick lab mesh generation failed for {failure.Key}: {failure.Error}");
		if (updates.ProcessedCount > 0)
		{
			_grid.ApplyFrame(_frame);
			_grid.GetNode<Node3D>("MovementRangeShell").Hide();
		}

		if (_frames.Interaction.MoveDestination is not null)
			return;

		var canPick = CanPick();
		var mouse = GetViewport().GetMousePosition();
		var previous = _frames.Interaction.MoveHoveredCell;
		var hovered = canPick
			? MovementSelection.PickCoordinate(
				_camera, GetViewport(), mouse,
				_frame.MovePaths, previous,
				_assistedPicking ? PreferenceFor : null)
			: null;
		var baseline = canPick && _assistedPicking
			? MovementSelection.PickCoordinate(_camera, GetViewport(), mouse, _frame.MovePaths, previous)
			: hovered;
		LogPick(hovered, baseline);
		if (hovered == _frames.Interaction.MoveHoveredCell)
			return;

		_frames.Interaction.SetMoveHover(hovered, _frame.MovePaths);
		RefreshFrame();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
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
					_message = "Pinned. Choose a heading, then Enter / Move here.";
				}
				else
					_message = "No preview under cursor. Hover a reachable destination first.";
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
		_message = "Hover to preview; click to pin a destination.";
		RefreshFrame();
	}

	private void ResetCamera()
	{
		var pose = BattleCameraPoses.PlayerAft(_battle.PlayerAgent.Sim.World.StateOf(_battle.PlayerId));
		_camera.SetOrbitFocus(pose.Pivot);
		_camera.SetFocus(pose.Pivot, pose.Distance, pose.Yaw, pose.Pitch);
	}

	private void CreateEncounter(Coord playerPosition, Coord fore, Coord dorsal, Coord enemyPosition)
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
					Position = enemyPosition,
					Fore = -Coord.Forward,
					ExecutionAgent = new AiController(),
				},
			],
		}, gridSize: 32);
		_battle.GrantPlayerCanWork();
	}

	private void RebuildLab()
	{
		_lastLoggedSnap = null;
		_frames = new PresentationFrameBuilder();
		_preview = new PlanningPreview();
		_preferences.Clear();
		_firingOpportunities.Clear();
		_frame = _frames.BuildFrame(_battle, _battle.PlayerAgent, acceptsCommands: true);
		_referenceEnemyId = "lab-enemy";
		_referenceEnemyPosition = _frame.PreviewUnits[_referenceEnemyId].Position;
		_optionsByDestination = _frame.MovePaths.Where(path => path.Steps.Count > 0)
			.GroupBy(path => path.EndPosition)
			.ToDictionary(group => group.Key, group => group.ToArray());
		_destinationCount = _optionsByDestination.Count;
		_destinationPreference = _preview.MovementPickPreferences(
			_battle.PlayerAgent.Sim, _battle.PlayerId, _referenceEnemyId, _frame.MovePaths);
		foreach (var coordinate in _optionsByDestination.Keys)
			PreferenceFor(coordinate);
		_highlightedCell = null;
		_cellColors.Clear();
		if (_coloredCells is not null)
		{
			RemoveChild(_coloredCells);
			_coloredCells.QueueFree();
		}
		BuildColoredCells();
		_units.ApplyUnitStates(_frame.PreviewUnits.ToDictionary(pair => pair.Key, pair => pair.Value.ToState()),
			id => _frame.PreviewUnitTeams[id] == ETeam.Player ? PlayerColor : EnemyColor);
		_targetMarker.Position = WorldMapping.ToWorld(_referenceEnemyPosition) + Vector3.Up;
		_camera.SetOrbitFocus(WorldMapping.ToWorld(_frame.FocusState.Position));
		ApplyFrame();
		GD.Print($"Movement pick lab ready: player {_frame.FocusState.Position}, enemy {_referenceEnemyPosition}, "
			+ $"{_destinationCount} destinations; {_preferences.Values.Count(preference => preference.CanHit)} firing destinations, "
			+ $"{_preferences.Values.Count(preference => preference.CanHit && preference.AlignedAxes == 2)} on enemy axis lines.");
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
			_message = "Move rejected by the simulation. Clear the selection and try again.";
			ApplyFrame();
			return;
		}
		var result = _battle.PlayerAgent.Sim.World.StateOf(_battle.PlayerId);
		var position = result.Position;
		var fore = result.Fore;
		var dorsal = result.Dorsal;
		// Each move starts a fresh sandbox encounter, restoring resources without changing live battle rules.
		CreateEncounter(position, fore, dorsal, _referenceEnemyPosition);
		_message = "Moved. Movement resources refreshed; pick the next destination.";
		RebuildLab();
		_cameraDirector.ReturnControl(WorldMapping.ToWorld(position));
	}

	public void RandomizeEnemy()
	{
		var player = _frame.FocusState;
		var candidates = Coord.OffsetsInCube(7)
			.Where(offset => offset.ManhattanDistanceTo(Coord.Zero) is >= 4 and <= 12)
			.Select(offset => player.Position + offset)
			.Where(cell => cell.X is >= 1 and < 31 && cell.Y is >= 1 and < 31 && cell.Z is >= 1 and < 31
				&& cell != _referenceEnemyPosition)
			.ToArray();
		if (candidates.Length == 0)
			throw new InvalidOperationException("No valid randomized enemy position in the lab grid.");
		var enemy = candidates[Random.Shared.Next(candidates.Length)];
		CreateEncounter(player.Position, player.Fore, player.Dorsal, enemy);
		_message = "Enemy randomized; ship and camera preserved.";
		RebuildLab();
	}

	public void ResetLab()
	{
		CreateEncounter(new Coord(16, 16, 16), Coord.Forward, Coord.Up, new Coord(19, 17, 22));
		_message = "Lab reset. Hover to preview; click to select.";
		RebuildLab();
		ResetCamera();
	}

	private void RefreshFrame()
	{
		_frame = _frames.BuildFrame(_battle, _battle.PlayerAgent, acceptsCommands: true);
		ApplyFrame();
	}

	private MovementPickPreference PreferenceFor(Coord coordinate)
	{
		if (_preferences.TryGetValue(coordinate, out var cached))
			return cached;

		var preference = _destinationPreference(coordinate);
		_preferences.Add(coordinate, preference);
		return preference;
	}

	private void LogPick(Coord? picked, Coord? baseline)
	{
		var snapped = _assistedPicking && picked != baseline && picked is Coord cell
			&& (PreferenceFor(cell).CanHit || PreferenceFor(cell).AlignedAxes == 2)
				? picked : null;
		if (_lastLoggedSnap == snapped)
			return;
		_lastLoggedSnap = snapped;
		GD.Print(snapped is Coord target
			? $"[movement-pick] SNAP {Format(target)} - "
				+ (PreferenceFor(target).CanHit ? "firing position (heading manual)" : "enemy axis line")
			: "[movement-pick] snap released");
	}

	private void ApplyFrame()
	{
		_moveButton.Disabled = _frame.SelectedMove is null;
		_grid.ApplyFrame(_frame);
		_grid.GetNode<Node3D>("MovementRangeShell").Hide();
		UpdateCellHighlight();
		_ghost.Apply(_frame.MoveGhostState, _frame.MoveCheckpoints, _frame.FocusState,
			_frame.ReachableMoveHeadings, PlayerColor, selected: _frame.SelectedMove is not null);
		var option = _frame.SelectedMove ?? _frame.HoveredMove;
		_readout.Text =
			$"Picker: {(_assistedPicking ? "firing-position assist" : "current battle algorithm")}"
			+ $" | {_destinationCount} destinations\n"
			+ $"Assist target: {_referenceEnemyId} {Format(_referenceEnemyPosition)}\n"
			+ $"Source: {Format(_frame.FocusState.Position)}\n"
			+ $"Hover: {Format(_frames.Interaction.MoveHoveredCell)}"
			+ $" | Pinned: {Format(_frames.Interaction.MoveDestination)}\n"
			+ (option is null
				? "Path: --\n"
				: $"Path: {option.Steps.Count} steps | AP: {option.ExtensionApCost}"
					+ $" | MP: {option.ExtensionMpCost} | Heading: {Format(option.EndBasis.Forward)}\n")
			+ (option is not null && _assistedPicking
				? $"Enemy axis line: {(PreferenceFor(option.EndPosition).AlignedAxes == 2 ? "yes" : "no")}"
					+ $" | Reachable firing heading: {(PreferenceFor(option.EndPosition).CanHit ? "yes" : "no")}\n"
					+ $"Current heading can hit: {(CanHitReference(option) ? "yes" : "no - choose heading manually")}\n"
				: "")
			+ _message;
	}

	private bool CanHitReference(MovePathOption option)
	{
		if (_firingOpportunities.TryGetValue(option, out var cached))
			return cached;

		var canHit = _preview.PoseHitOpportunities(_battle.PlayerAgent.Sim, _battle.PlayerId, option)
			.Any(opportunity => opportunity.TargetId == _referenceEnemyId);
		_firingOpportunities.Add(option, canHit);
		return canHit;
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
			Text = "Movement picking lab\n"
				+ "Immediate movement; resources refresh after each move. Enemy stays still.\n"
				+ "Use your battle camera bindings to orbit / pan / zoom.\n"
				+ "Left click: select / heading | Enter: move | Esc: clear | R: reset camera",
			MouseFilter = Control.MouseFilterEnum.Ignore,
		});
		_readout = new Label { MouseFilter = Control.MouseFilterEnum.Ignore };
		column.AddChild(_readout);
		var assist = new CheckButton
		{
			Text = "Firing-position assist (heading stays manual)",
			TooltipText = "Firing destinations / enemy axis lines attract within 18 pixels of the closest cell "
				+ "and release beyond 24 pixels. Other alignment uses a weak 6-pixel tie-break.\n"
				+ "Direct aim within 1 pixel selects the closest centre. No automatic heading changes.",
			ButtonPressed = _assistedPicking,
		};
		assist.Toggled += enabled =>
		{
			_assistedPicking = enabled;
			ClearSelection();
		};
		column.AddChild(assist);
		var colors = new CheckButton
		{
			Text = "Per-cell colors (bright cell = hovered / pinned)",
			ButtonPressed = true,
		};
		colors.Toggled += enabled =>
		{
			_showCellColors = enabled;
			_coloredCells.Visible = enabled;
		};
		column.AddChild(colors);
		column.AddChild(new Label
		{
			Text = "Axis-line cells: green = firing heading available | gold = alignment only",
			MouseFilter = Control.MouseFilterEnum.Ignore,
		});
		column.AddChild(new Label
		{
			Text = "Dotted bearing guides: X red | Y green | Z blue",
			MouseFilter = Control.MouseFilterEnum.Ignore,
		});
		var buttons = new HBoxContainer();
		column.AddChild(buttons);
		var clear = new Button { Text = "Clear selection", FocusMode = Control.FocusModeEnum.None };
		clear.Pressed += ClearSelection;
		buttons.AddChild(clear);
		var reset = new Button { Text = "Reset camera", FocusMode = Control.FocusModeEnum.None };
		reset.Pressed += ResetCamera;
		buttons.AddChild(reset);
		var actions = new HBoxContainer();
		column.AddChild(actions);
		_moveButton = new Button
		{
			Text = "Move here (Enter)", Disabled = true, FocusMode = Control.FocusModeEnum.None,
		};
		_moveButton.Pressed += MoveHere;
		actions.AddChild(_moveButton);
		var randomize = new Button { Text = "Randomize enemy", FocusMode = Control.FocusModeEnum.None };
		randomize.Pressed += RandomizeEnemy;
		actions.AddChild(randomize);
		var resetLab = new Button { Text = "Reset lab", FocusMode = Control.FocusModeEnum.None };
		resetLab.Pressed += ResetLab;
		actions.AddChild(resetLab);
	}

	private void BuildColoredCells()
	{
		_coloredCells = new Node3D { Name = "ColoredDestinations", Visible = _showCellColors };
		AddChild(_coloredCells);
		// Inset the cubes so adjacent cells do not share overlapping colored faces.
		var box = new BoxMesh { Size = Vector3.One * WorldMapping.CellSize * 0.94f };
		var outline = CellGridGeometry.CreateMesh(
			Vector3.One.Normalized(), [Vector3.Zero], includeHatches: false,
			new CellGridGeometry.LineAppearance(1f, 1f, 1f));
		var coordinates = _frame.MovePaths.Select(path => path.EndPosition)
			.Distinct().OrderBy(cell => cell.X).ThenBy(cell => cell.Y).ThenBy(cell => cell.Z)
			.ToArray();
		for (var i = 0; i < coordinates.Length; i++)
		{
			var coordinate = coordinates[i];
			var preference = PreferenceFor(coordinate);
			var color = preference.AlignedAxes == 2
				? preference.CanHit ? new Color(0.3f, 1f, 0.45f) : Colors.Gold
				: Color.FromHsv(Mathf.PosMod(i * 0.618034f, 1f), 0.55f, 0.85f);
			var fillMaterial = CellGridGeometry.CreateMaterial(new Color(color, 0.045f));
			var outlineMaterial = CellGridGeometry.CreateMaterial(
				new Color(color, preference.AlignedAxes == 2 ? 0.85f : 0.3f));
			var cell = new Node3D
			{
				Name = $"Cell_{coordinate.X}_{coordinate.Y}_{coordinate.Z}",
				Position = WorldMapping.ToWorld(coordinate),
			};
			_coloredCells.AddChild(cell);
			var fill = new MeshInstance3D
			{
				Mesh = box,
				MaterialOverride = fillMaterial,
				CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			};
			var edges = new MeshInstance3D
			{
				Mesh = outline,
				MaterialOverride = outlineMaterial,
				Scale = Vector3.One * 0.94f,
				CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			};
			PresentationLayers.MarkUx(fill);
			PresentationLayers.MarkUx(edges);
			cell.AddChild(fill);
			cell.AddChild(edges);
			_cellColors.Add(coordinate, (color, fillMaterial, outlineMaterial));
		}
	}

	private void UpdateCellHighlight()
	{
		var highlighted = _frame.SelectedMove?.EndPosition ?? _frame.HoveredMove?.EndPosition;
		if (highlighted == _highlightedCell)
			return;

		if (_highlightedCell is Coord previous)
		{
			var old = _cellColors[previous];
			old.Fill.AlbedoColor = new Color(old.Color, 0.045f);
			old.Outline.AlbedoColor = new Color(old.Color,
				PreferenceFor(previous).AlignedAxes == 2 ? 0.85f : 0.3f);
		}
		if (highlighted is Coord current)
		{
			var active = _cellColors[current];
			active.Fill.AlbedoColor = new Color(active.Color, 0.24f);
			active.Outline.AlbedoColor = new Color(active.Color.Lerp(Colors.White, 0.7f), 1f);
		}
		_highlightedCell = highlighted;
	}

	private static string Format(Coord? coordinate) =>
		coordinate is Coord cell ? $"({cell.X}, {cell.Y}, {cell.Z})" : "--";
}

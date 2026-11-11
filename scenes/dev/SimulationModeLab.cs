using Godot;
using GrimSpace.Battle;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Presentation.Camera;
using GrimSpace.Battle.Presentation.Domains.Move;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Battle.Presentation.Picking;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Units.Enums;

namespace GrimSpace.Scenes.Dev;

/// <summary>Repeatable battle presentation sandbox; interactions never enqueue or execute actions.</summary>
public partial class SimulationModeLab : Node3D
{
	private const int Seed = 42;
	private readonly PresentationFrameBuilder _frames = new();
	private BattleOrchestrator _battle = null!;
	private Controller _camera = null!;
	private BattleView _units = null!;
	private BoardAsteroidView _asteroids = null!;
	private GridView _grid = null!;
	private MoveGhostView _ghost = null!;
	private RelationshipGuideView _targeting = null!;
	private RelationshipGuideView.ReferencePlane _referencePlane;
	private CellVolumeMeshStore _meshes = null!;
	private PresentationFrame _frame = null!;
	private string[] _enemies = [];

	public override void _Ready()
	{
		_battle = BattleOrchestrator.FromEncounter(BattleEncounter.DevDefault(Seed));
		_battle.GrantPlayerCanWork();
		var layout = _battle.Layout;
		_frame = _frames.BuildFrame(_battle, _battle.PlayerAgent, acceptsCommands: true);
		_enemies = _frame.PreviewUnits.Keys
			.Where(id => _frame.PreviewUnitTeams[id] == ETeam.Enemy)
			.OrderBy(id => BattleDistance.SeparationMeters(
				_frame.FocusState.Position, _frame.PreviewUnits[id].Position))
			.ThenBy(id => id, StringComparer.Ordinal)
			.ToArray();
		if (_enemies.Length == 0)
			throw new InvalidOperationException("Simulation mode lab requires an enemy target.");
		_frames.Interaction.SetRangeEnemy(_enemies[0]);

		_camera = new Controller { Name = "Camera3D", Current = true, Fov = 55f };
		AddChild(_camera);
		ResetCamera();

		var backdrop = new SpaceBackdrop { Name = "SpaceBackdrop" };
		AddChild(backdrop);
		backdrop.Build(layout.Grid, Seed, _camera);
		var light = new DirectionalLight3D { Name = "SunLight" };
		AddChild(light);
		RedDwarfSun.Configure(light, WorldMapping.GridCenter(layout.Grid),
			layout.Grid.Width * WorldMapping.CellSize * 0.5f);

		_asteroids = new BoardAsteroidView { Name = "Asteroids" };
		AddChild(_asteroids);
		_asteroids.Build(_camera, layout.Asteroids);
		_units = new BattleView
		{
			Name = "Ships",
			ShieldTintForActor = id => ShieldFaceMaterials.TeamTint(_frame.PreviewUnitTeams[id]),
		};
		AddChild(_units);
		_units.BindInitial(_frame.PreviewUnits.Select(pair =>
			(pair.Key, pair.Value.ToState(), ColorFor(pair.Key))));

		_meshes = new CellVolumeMeshStore();
		_grid = new GridView { Name = "MovementGrid" };
		AddChild(_grid);
		_grid.Build(_camera, _meshes);
		_ghost = new MoveGhostView { Name = "MoveGhost" };
		_ghost.Configure(_camera);
		AddChild(_ghost);
		_targeting = new RelationshipGuideView { Name = "Targeting" };
		AddChild(_targeting);
		RefreshFrame();
		GD.Print("Simulation mode lab: fixed battle, no HUD or execution. "
			+ "Battle camera bindings; click enemy to target; hover / click space to preview / pin; "
			+ "Tab cycles enemies; 1 / 2 / 3 select XZ / YZ / XY; Esc clears preview; R resets camera.");
	}

	public override void _Process(double delta)
	{
		var updates = _meshes.Pump(_frame.SimulationTick);
		foreach (var failure in updates.Failures)
			GD.PushError($"Simulation mode lab mesh generation failed for {failure.Key}: {failure.Error}");
		if (updates.ProcessedCount > 0)
			_grid.ApplyFrame(_frame);

		if (_frames.Interaction.MoveDestination is not null)
			return;
		var hovered = CanPick()
			? MovementSelection.PickCoordinate(_camera, GetViewport(), GetViewport().GetMousePosition(),
				_frame.MovePaths, _frames.Interaction.MoveHoveredCell)
			: null;
		if (hovered == _frames.Interaction.MoveHoveredCell)
			return;
		_frames.Interaction.SetMoveHover(hovered, _frame.MovePaths);
		RefreshFrame();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey { Pressed: true, Echo: false } key)
		{
			switch (key.Keycode)
			{
				case Key.Key1:
					_referencePlane = RelationshipGuideView.ReferencePlane.XZ;
					break;
				case Key.Key2:
					_referencePlane = RelationshipGuideView.ReferencePlane.YZ;
					break;
				case Key.Key3:
					_referencePlane = RelationshipGuideView.ReferencePlane.XY;
					break;
				case Key.Escape:
					_frames.Interaction.ClearMoveSelection();
					_frames.Interaction.ClearHovers();
					break;
				case Key.Tab:
					var index = Array.IndexOf(_enemies, _frame.RangeEnemy?.EnemyId);
					_frames.Interaction.SetRangeEnemy(_enemies[(index + 1) % _enemies.Length]);
					break;
				case Key.R:
					ResetCamera();
					break;
				default:
					return;
			}
		}
		else if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } click
			&& CanPick())
		{
			var picked = UnitPick.Pick(_camera, click.Position, _units.UnitViews);
			if (picked is not null && _enemies.Contains(picked))
				_frames.Interaction.SetRangeEnemy(picked);
			else if (_frame.HoveredMove is { } option)
				_frames.Interaction.BeginMoveSelection(option.EndPosition, option.EndBasis);
		}
		else
			return;

		RefreshFrame();
		GetViewport().SetInputAsHandled();
	}

	private bool CanPick() =>
		!_camera.IsManualGestureActive && GetViewport().GuiGetHoveredControl() is null;

	private void ResetCamera()
	{
		var pose = BattleCameraPoses.PlayerAft(_frame.FocusState.ToState());
		_camera.SetOrbitFocus(WorldMapping.GridCenter(_battle.Layout.Grid));
		_camera.SetFocus(pose.Pivot, pose.Distance, pose.Yaw, pose.Pitch);
	}

	private Color ColorFor(string id) =>
		_frame.PreviewUnitTeams[id] == ETeam.Enemy
			? new Color(0.9f, 0.25f, 0.2f)
			: new Color(0.25f, 0.85f, 0.35f);

	private void RefreshFrame()
	{
		_frame = _frames.BuildFrame(_battle, _battle.PlayerAgent, acceptsCommands: true);
		_grid.ApplyFrame(_frame);
		_ghost.Apply(_frame.MoveGhostState, _frame.MoveCheckpoints, _frame.FocusState,
			_frame.ReachableMoveHeadings, ColorFor(_frame.FocusId),
			selected: _frame.SelectedMove is not null);
		_ghost.GetNode<Node3D>("DottedAxisGuides").Hide();
		var source = _frame.MoveGhostState ?? _frame.FocusState;
		if (_frame.RangeEnemy is not { } target)
			throw new InvalidOperationException("Simulation mode lab lost its selected enemy.");
		_targeting.Apply(
			source.Position,
			_frame.PreviewUnits[target.EnemyId].Position,
			_referencePlane);
		_asteroids.SetOcclusionTargets(
		[
			WorldMapping.ToWorld(_frame.FocusState.Position),
			WorldMapping.ToWorld(source.Position),
			WorldMapping.ToWorld(_frame.PreviewUnits[target.EnemyId].Position),
		]);
	}

	public override void _ExitTree()
	{
		_meshes?.Dispose();
		_battle?.Dispose();
	}
}

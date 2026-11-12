using Godot;
using GrimSpace.Application;
using GrimSpace.Battle;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Encounter.Generation;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Objectives;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Presentation.Camera;
using GrimSpace.Battle.Presentation.Domains.Move;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Battle.Presentation.Interaction;
using GrimSpace.Battle.Presentation.Picking;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.World.Factions;
using GrimSpace.Core.Actions;

namespace GrimSpace.Scenes.Dev;

/// <summary>
/// Repeatable planning sandbox; no live execution. Button/B glitches between normal and simulation visuals.
/// Use --lab-gooper or --lab-rogue to try movement abilities.
/// </summary>
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
	private Node3D _afterimages = null!;
	private readonly List<MovementAfterimageView> _movementPaths = [];
	private IReadOnlyList<IReadOnlyList<MoveCheckpoint>> _queuedMovePaths = [];
	private MovementAfterimageView _abilityOrigins = null!;
	private UnitDisplayState? _abilityAimResult;
	private RelationshipGuideView _targeting = null!;
	private EnemyRangeTetherView _targetInfo = null!;
	private RelationshipGuideView.ReferencePlane _referencePlane;
	private CellVolumeMeshStore _meshes = null!;
	private SimulationModePresentation _simulationPresentation = null!;
	private SimulationModeTransitionView _simulationTransition = null!;
	private SimulationBackdropView _simulationBackdrop = null!;
	private PresentationFrame _frame = null!;
	private string[] _enemies = [];
	private bool _blueprintEnabled = true;
	private AreaActionPreviewView _weaponPreviews = null!;
	private VoidBombPreviewView _voidBombPreviews = null!;
	private AbilitySourcePickerView _abilitySources = null!;
	private TargetOpportunityOverlay _opportunities = null!;
	private Button _simulationToggle = null!;

	public override void _Ready()
	{
		_battle = BattleOrchestrator.PrepareFromEncounter(CreateEncounter());
		_battle.EnterPlayerTurn("simulation lab");
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

		_simulationBackdrop = new SimulationBackdropView { Name = "SimulationBackdrop" };
		AddChild(_simulationBackdrop);
		_simulationBackdrop.Build(layout.Grid, Seed, _camera);
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
		_weaponPreviews = new AreaActionPreviewView { Name = "WeaponPreviews" };
		_weaponPreviews.Build(_meshes);
		AddChild(_weaponPreviews);
		_voidBombPreviews = new VoidBombPreviewView { Name = "VoidBombPreviews" };
		_voidBombPreviews.Build(_meshes);
		AddChild(_voidBombPreviews);
		_abilitySources = new AbilitySourcePickerView { Name = "AbilitySources" };
		_abilitySources.Configure(_camera);
		AddChild(_abilitySources);
		var opportunityLayer = new CanvasLayer { Layer = 20 };
		AddChild(opportunityLayer);
		_opportunities = new TargetOpportunityOverlay();
		_opportunities.Configure(_camera);
		opportunityLayer.AddChild(_opportunities);
		_simulationPresentation = new SimulationModePresentation(
			_simulationBackdrop.TintMaterial,
			_simulationBackdrop.WorldEnvironment,
			_asteroids.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>(),
			new[] { "MovementRangeShell", "MovementLocalGrid", "MovementCentralOutline" }
				.Select(name => (StandardMaterial3D)_grid.GetNode<MeshInstance3D>(name).MaterialOverride));
		_ghost = new MoveGhostView { Name = "MoveGhost" };
		_ghost.Configure(_camera);
		AddChild(_ghost);
		_afterimages = new Node3D { Name = "MovementAfterimages" };
		AddChild(_afterimages);
		_abilityOrigins = new MovementAfterimageView { Name = "AbilityOrigins" };
		AddChild(_abilityOrigins);
		_targeting = new RelationshipGuideView
		{
			Name = "Targeting",
			ShowReticle = false,
			AnimateAlignment = true,
		};
		AddChild(_targeting);
		_targetInfo = new EnemyRangeTetherView { Name = "TargetInfo", ShowRangeGuides = false };
		_targetInfo.Configure(layout.Grid);
		AddChild(_targetInfo);
		RefreshFrame();
		BuildSimulationToggle();
		if (OS.GetCmdlineUserArgs().Contains("--preview-afterimages"))
		{
			var position = _frame.FocusState.Position + _frame.FocusState.Fore * 4;
			var move = _frame.MovePaths.First(path => path.EndPosition == position);
			_frames.Interaction.BeginMoveSelection(move.EndPosition, move.EndBasis);
			RefreshFrame();
		}
		GD.Print("Simulation mode lab: fixed battle, no HUD or turn execution. "
			+ "Battle camera bindings; click enemy to target; hover / click space to preview / pin; "
			+ "Tab cycles enemies; 1 / 2 / 3 select XZ / YZ / XY; "
			+ "B toggles blueprint visuals; Esc cancels preview; R resets camera. "
			+ "M: movement; W: cycle abilities; Q/E: cycle mounts or roll pinned ghost; "
			+ "Enter: queue preview; Z: undo; Delete: clear queued plan.");
		foreach (var spec in AbilityHudCatalog.ForDisplayState(_frame.FocusState))
			GD.Print($"Lab ability: {spec.Mode}");
		foreach (var id in _enemies)
		{
			var distance = BattleDistance.SeparationMeters(
				_frame.FocusState.Position, _frame.PreviewUnits[id].Position);
			GD.Print($"Simulation lab target {id}: {BattleDistance.Format(distance)}, "
				+ (distance < RelationshipGuideView.AlignmentDistanceMeters
					? "plane alignment"
					: "single tether"));
		}
	}

	private void BuildSimulationToggle()
	{
		_simulationTransition = new SimulationModeTransitionView { Name = "SimulationTransition" };
		AddChild(_simulationTransition);
		var controls = new CanvasLayer { Name = "SimulationControls", Layer = 100 };
		AddChild(controls);
		_simulationToggle = new Button
		{
			Name = "SimulationToggle",
			Text = "Simulation [B]",
			ToggleMode = true,
			ButtonPressed = _blueprintEnabled,
			Position = new Vector2(20f, 20f),
			CustomMinimumSize = new Vector2(180f, 42f),
			FocusMode = Control.FocusModeEnum.None,
			TooltipText = "Toggle simulation visuals",
		};
		_simulationToggle.Pressed += ToggleSimulation;
		controls.AddChild(_simulationToggle);
	}

	private void ToggleSimulation()
	{
		if (_simulationTransition.IsCapturing)
		{
			_simulationToggle.SetPressedNoSignal(_blueprintEnabled);
			return;
		}
		var target = !_blueprintEnabled;
		_simulationTransition.Transition(() =>
		{
			_blueprintEnabled = target;
			_simulationToggle.SetPressedNoSignal(target);
			RefreshFrame();
		});
	}

	private static BattleEncounter CreateEncounter()
	{
		var arguments = OS.GetCmdlineUserArgs();
		var playerType = arguments.Contains("--lab-gooper") ? EType.IndustrialGooper
			: arguments.Contains("--lab-rogue") ? EType.Rogue : EType.Fighter;
		BattleSpawn[] spawns =
		[
			new()
			{
				Ship = ShipInstance.FromCatalog("lab-player", playerType),
				Team = ETeam.Player,
				Faction = EFaction.Player,
				Position = new Coord(32, 32, 20),
				ExecutionAgent = new UserExecutionAgent(),
			},
			new()
			{
				Ship = ShipInstance.FromCatalog("lab-close-ahead", EType.Fighter),
				Team = ETeam.Enemy,
				Faction = EFaction.TheOptimality,
				Position = playerType == EType.IndustrialGooper ? new Coord(32, 32, 23) : new Coord(34, 33, 26),
				Fore = -Coord.Forward,
				ExecutionAgent = new AiController(),
			},
			new()
			{
				Ship = ShipInstance.FromCatalog("lab-far-ahead", EType.Fighter),
				Team = ETeam.Enemy,
				Faction = EFaction.TheOptimality,
				Position = new Coord(35, 34, 50),
				Fore = -Coord.Forward,
				ExecutionAgent = new AiController(),
			},
			new()
			{
				Ship = ShipInstance.FromCatalog("lab-close-below", EType.Fighter),
				Team = ETeam.Enemy,
				Faction = EFaction.TheOptimality,
				Position = new Coord(30, 25, 22),
				Fore = Coord.Up,
				Dorsal = Coord.Forward,
				ExecutionAgent = new AiController(),
			},
		];
		return new BattleEncounter
		{
			Id = "simulation-mode-lab",
			Seed = Seed,
			Objective = EObjective.EliminateOpponents,
			Spawns = spawns,
			WorldHazards = AsteroidFieldGenerator.Generate(new AsteroidFieldConfig
			{
				Seed = Seed,
				UnitPositions = spawns.Select(spawn => spawn.Position).ToArray(),
				ReservedCells = AsteroidFieldReservations.PlayerCorridors(spawns),
			}),
		};
	}

	public override void _Process(double delta)
	{
		_simulationBackdrop.SyncCamera(_camera);
		var updates = _meshes.Pump(_frame.SimulationTick);
		foreach (var failure in updates.Failures)
			GD.PushError($"Simulation mode lab mesh generation failed for {failure.Key}: {failure.Error}");
		if (updates.ProcessedCount > 0)
			ApplyPreviewVisuals();

		if (_frame.Mode != EPlayerMode.Move)
		{
			if (CanPick() && _abilitySources.PickIndex(GetViewport().GetMousePosition()) is { } index
				&& index != _frame.AbilityHoveredIndex)
			{
				_frames.Interaction.SetAbilityHover(index, _frame.AbilityChoices.Count);
				RefreshFrame();
			}
			return;
		}

		if (_frames.Interaction.MoveDestination is not null)
		{
			if (CanPick() && TrySelectHeading(GetViewport().GetMousePosition()))
				RefreshFrame();
			return;
		}
		var hovered = CanPick()
			? MovementSelection.PickCoordinate(_camera, GetViewport(), GetViewport().GetMousePosition(),
				_frame.MovePaths, _frames.Interaction.MoveHoveredCell, _frame.MovementPickPreference)
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
				case Key.M:
					_frames.Interaction.SetMode(EPlayerMode.Move);
					break;
				case Key.W:
					var specs = AbilityHudCatalog.ForDisplayState(_frame.FocusState);
					var current = specs.ToList().FindIndex(spec => spec.Mode == _frame.Mode);
					var next = specs.Skip(current + 1).Concat(specs.Take(current + 1))
						.FirstOrDefault(spec => spec.IsLegal(_frame.Abilities));
					if (next is null)
						GD.Print("Lab: no legal ability available; undo or clear the plan to restore resources.");
					else
					{
						_frames.Interaction.SetMode(next.Mode, next);
						RefreshFrame();
						_frames.Interaction.SetAbilityHover(0, _frame.AbilityChoices.Count);
						GD.Print($"Lab preview: {next.Mode}. Q/E select a mount; Enter queues.");
					}
					break;
				case Key.Q:
				case Key.E:
					var delta = key.Keycode == Key.Q ? -1 : 1;
					if (_frame.SelectedMove is { } move)
					{
						var bases = _frame.MovePaths.Where(path => path.EndPosition == move.EndPosition)
							.Select(path => path.EndBasis).ToArray();
						if (MovePose.CycleRoll(bases, move.EndBasis, delta) is { } basis)
							_frames.Interaction.SetMovePose(basis);
					}
					else if (_frame.AbilityChoices.Count > 0)
						_frames.Interaction.SetAbilityHover(
							((_frame.AbilityHoveredIndex ?? 0) + delta + _frame.AbilityChoices.Count)
								% _frame.AbilityChoices.Count,
							_frame.AbilityChoices.Count);
					break;
				case Key.Enter:
				case Key.KpEnter:
					QueuePreview();
					break;
				case Key.Z:
				case Key.Delete:
					_frames.Interaction.SetMode(EPlayerMode.Move);
					if (key.Keycode == Key.Delete)
					{
						while (_battle.PlayerAgent.CanUndo)
							if (!_battle.PlayerAgent.Undo())
								throw new InvalidOperationException("Lab could not clear an undoable plan.");
					}
					else if (!_battle.PlayerAgent.Undo())
						GD.Print("Lab: no queued action to undo.");
					break;
				case Key.B:
					ToggleSimulation();
					break;
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
					_frames.Interaction.SetMode(EPlayerMode.Move);
					break;
				case Key.Tab:
					if (_frame.TargetPick is { LegalIds.Count: > 0 } targets)
					{
						var candidate = targets.LegalIds.ToList().IndexOf(targets.CandidateId ?? "");
						_frames.Interaction.SetTargetCandidate(
							targets.LegalIds[(candidate + 1) % targets.LegalIds.Count]);
					}
					else
					{
						var enemies = _enemies.Where(id =>
							_frame.PreviewUnits.TryGetValue(id, out var enemy) && enemy.IsAlive).ToArray();
						if (enemies.Length > 0)
						{
							var index = Array.IndexOf(enemies, _frame.RangeEnemy?.EnemyId);
							_frames.Interaction.SetRangeEnemy(enemies[(index + 1) % enemies.Length]);
						}
					}
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
			if (_frame.TargetPick is { } pick)
			{
				if (picked is not null && pick.LegalIds.Contains(picked))
					_frames.Interaction.SetTargetCandidate(picked);
			}
			else if (_frame.Mode != EPlayerMode.Move)
			{
				if (_abilitySources.PickIndex(click.Position) is { } index)
					_frames.Interaction.SetAbilityHover(index, _frame.AbilityChoices.Count);
			}
			else if (_frame.SelectedMove is not null)
			{
				if (TrySelectHeading(click.Position))
				{
					RefreshFrame();
					QueuePreview();
				}
			}
			else if (picked is not null && _enemies.Contains(picked))
				_frames.Interaction.SetRangeEnemy(picked);
			else if (_frame.HoveredMove is { } option)
				_frames.Interaction.BeginMoveSelection(option.EndPosition, option.EndBasis);
		}
		else
			return;

		RefreshFrame();
		GetViewport().SetInputAsHandled();
	}

	private bool TrySelectHeading(Vector2 pointer)
	{
		if (_frame.SelectedMove is not { } selected
			|| MovementSelection.PickHeadingHandle(_camera, pointer,
				selected.EndPosition, _frame.ReachableMoveHeadings) is not { } heading)
			return false;
		var bases = _frame.MovePaths.Where(path => path.EndPosition == selected.EndPosition)
			.Select(path => path.EndBasis).ToArray();
		if (MovePose.SelectHeading(bases, selected.EndBasis, heading) is not { } basis)
			return false;
		_frames.Interaction.SetMovePose(basis);
		return true;
	}

	private void QueuePreview()
	{
		IReadOnlyList<IAction> actions;
		if (_frame.SelectedMove is { } move)
			actions = move.Steps.Cast<IAction>().ToArray();
		else if (_frame.TargetPick is { } pick)
		{
			if (pick.CanConfirmPick)
			{
				_frames.Interaction.ConfirmTargetPick();
				RefreshFrame();
			}
			if (_frame.TargetPick is not { CanLockIn: true } ready)
			{
				GD.Print("Lab: select the required targets before queuing.");
				return;
			}
			if (_frames.Interaction.ActiveAbilitySpec?.RequiresFacePickAfterTargets == true)
			{
				_frames.Interaction.BeginFacePick(ready.PickedIds.Single());
				RefreshFrame();
				_frames.Interaction.SetAbilityHover(0, _frame.AbilityChoices.Count);
				return;
			}
			actions = [ready.Def.Bind(_battle.PlayerId, ready.PickedIds)];
		}
		else if (_frame.HoveredAbilityChoice is { } choice)
			actions = [AbilityActivation.CreateExecutionAction(choice)];
		else
		{
			GD.Print("Lab: pin a movement destination or select an ability/mount first.");
			return;
		}

		if (!_battle.PlayerAgent.TryEnqueue(actions))
		{
			_frames.Interaction.ReportActionFailure();
			GD.PushWarning("Lab: simulation rejected the queued preview; no live action was executed.");
			return;
		}
		_frames.Interaction.SetMode(EPlayerMode.Move);
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
		var displacement = PlanningPreview.AbilityDisplacements(
			_battle.PlayerAgent.Sim, _frame.FocusId,
			_frame.ShowWeaponPreviews ? _frame.HoveredAbilityChoice?.Action : null);
		_abilityAimResult = displacement.AimResult;
		_queuedMovePaths = displacement.MovePaths;
		_abilityOrigins.ApplyOrigins(displacement.Origins);
		var states = _frame.PreviewUnits.ToDictionary(pair => pair.Key, pair => pair.Value.ToState());
		if (_abilityAimResult is { } aim)
			states[aim.Id] = aim.ToState();
		_units.ApplyPlanningPreview(
			states, ColorFor);
		ApplyPreviewVisuals();
	}

	private void ApplyPreviewVisuals()
	{
		_grid.ApplyFrame(_frame);
		_weaponPreviews.ApplyFrame(_frame);
		_voidBombPreviews.ApplyFrame(_frame);
		_abilitySources.Apply(_frame.AbilityChoices, _frame.AbilityHoveredIndex,
			showHoveredGhost: _abilityAimResult is null);
		_opportunities.Apply(_frame.PreviewUnits, _frame.PoseHitOpportunities);
		_ghost.Apply(_frame.MoveGhostState, _blueprintEnabled ? [] : _frame.MoveCheckpoints, _frame.FocusState,
			_frame.ReachableMoveHeadings, ColorFor(_frame.FocusId),
			selected: _frame.SelectedMove is not null);
		_afterimages.Visible = _blueprintEnabled;
		if (_blueprintEnabled)
		{
			var paths = _queuedMovePaths.ToList();
			if (_frame.SelectedMove is { } selectedMove)
				paths.Add(selectedMove.Checkpoints);
			while (_movementPaths.Count < paths.Count)
			{
				var view = new MovementAfterimageView { Name = $"MovementPath{_movementPaths.Count}" };
				_afterimages.AddChild(view);
				_movementPaths.Add(view);
			}
			for (var i = 0; i < _movementPaths.Count; i++)
			{
				_movementPaths[i].Visible = i < paths.Count;
				if (i < paths.Count)
					_movementPaths[i].Apply(paths[i], _frame.FocusState);
			}
		}
		_ghost.GetNode<Node3D>("DottedAxisGuides").Hide();
		var source = _frame.MoveGhostState ?? _abilityAimResult ?? _frame.FocusState;
		_simulationPresentation.Apply(
			_blueprintEnabled,
			_units.UnitViews,
			_frame.PreviewUnitTeams,
			_battle.PlayerId,
			_frame.RangeEnemy?.EnemyId);
		_targeting.Visible = _frame.RangeEnemy is not null;
		if (_frame.RangeEnemy is not { } target)
		{
			_targetInfo.Apply(null, null, 0);
			_asteroids.SetOcclusionTargets([WorldMapping.ToWorld(source.Position)]);
			return;
		}
		_targeting.Apply(
			source.Position,
			_frame.PreviewUnits[target.EnemyId].Position,
			_referencePlane,
			showAlignment: BattleDistance.SeparationMeters(
				(_abilityAimResult ?? _frame.FocusState).Position,
				_frame.PreviewUnits[target.EnemyId].Position)
				< RelationshipGuideView.AlignmentDistanceMeters);
		_targetInfo.Apply(
			_ghost.GhostShip ?? _units.UnitViews[_battle.PlayerId],
			_units.UnitViews[target.EnemyId],
			BattleDistance.SeparationMeters(source.Position, _frame.PreviewUnits[target.EnemyId].Position),
			source,
			_frame.PreviewUnits[target.EnemyId],
			damage: _frame.AbilityDamage.GetValueOrDefault(target.EnemyId));
		_asteroids.SetOcclusionTargets(
		[
			WorldMapping.ToWorld(_frame.FocusState.Position),
			WorldMapping.ToWorld(source.Position),
			WorldMapping.ToWorld(_frame.PreviewUnits[target.EnemyId].Position),
		]);
	}

	public override void _ExitTree()
	{
		_simulationPresentation?.Dispose();
		_meshes?.Dispose();
		_battle?.Dispose();
	}
}

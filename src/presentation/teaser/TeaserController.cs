using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using GrimSpace.Application;
using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Encounter.Generation;
using GrimSpace.Battle.Objectives;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Presentation.Scene;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Ids;
using GrimSpace.Math.Camera;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Presentation.Camera;
using GrimSpace.World.StarSystem.Presentation.Scene;
using GrimSpace.World.StarSystem.Presentation.Ui;
using GrimSpace.World.StarSystem.Resources;
using RunState = GrimSpace.Run.State;

namespace GrimSpace.Presentation.Teaser;

/// <summary>
/// Directs the teaser video (V4 pass: no text, systems first).
///
/// The cut has no captions, no tags and no editorial words at all: the only
/// text the film ever shows is the game's own interface, plus the closing
/// name card. Every beat is live game content recorded from the current code:
///
///   PLAN     the tactical console: turn order, the action cards, the command
///            wing, the reachable-cell bubble and the move ghost
///   EXECUTE  the order resolves: a hull accelerates away (movement trail),
///            a weapon lands, a hull is lost (the boom)
///   VARIETY  a second, different engagement: another chassis, another
///            weapon, another field, another camera
///   STAR     the run layer: the star map, the fleet travelling a route
///   CONTRACT the contract board: a contract offered, then accepted
///   DOCK     the dockyard: a hull upgrade offered and bought
///   FLEET    the fleet and its pilot: the progression screens
///   TITLE    the name
///
/// The battle runs in its own viewport so the world beats can play over the
/// top of a fight that never stops. Transitions are hard cuts between the
/// battle and the world and cross-dissolves inside the world; the frame never
/// passes through black.
/// </summary>
public partial class TeaserController : Node
{
	// ---- the cut ------------------------------------------------------------
	private const float PlanSeconds = 3.8f;
	private const float ExecuteSeconds = 5.4f;
	private const float VarietySeconds = 5.4f;
	private const float StarSeconds = 5.0f;
	private const float ContractSeconds = 4.0f;
	private const float DockSeconds = 3.8f;
	private const float FleetSeconds = 3.4f;
	private const float TitleSeconds = 3.4f;
	private const float DissolveSeconds = 0.4f;
	private const float OpeningFadeSeconds = 0.35f;
	private const float EndFadeSeconds = 1.0f;

	// The plan shot is close enough that the hull is a subject and the reachable
	// cells read as a slab of space the ship may cross, not as a wall of wire.
	// The plan shot is close enough that the hull is a subject, and it is held
	// on the weapon rather than on the move range: the range is a wall of wire,
	// the console is the point.
	private const float PlanDistance = 8.5f;
	private const float PlanYaw = 0.18f;
	private const float PlanPitch = 0.16f;
	private const float PlanRoll = -0.12f;

	// The execution shot watches the target, not the shooter: the hull
	// accelerates into frame, the shot lands, and the loss happens at the
	// camera.
	private const float ExecuteDistance = 9.5f;
	private const float ExecuteYaw = 0.30f;
	private const float ExecutePitch = 0.20f;
	private const float ExecuteRoll = -0.09f;

	// The variety shot is a different language: further out, lower and rolling
	// the other way.
	private const float VarietyDistance = 13f;
	private const float VarietyYaw = -0.55f;
	private const float VarietyPitch = 0.34f;
	private const float VarietyRoll = 0.13f;

	// The closing card: a wide, level look at a fight that is still running.
	private const float TitleDistance = 15f;
	private const float TitleYaw = 0.42f;
	private const float TitlePitch = 0.26f;
	private const float TitleRoll = 0.04f;

	// The reveal. The camera retreats because a hull was just lost.
	private const float RevealRoll = 0.11f;
	private const float RevealDollySeconds = 0.9f;
	private const float RevealOrbitAmplitude = 0.08f;
	private const float RevealDistance = 14f;

	private const int PlanSeed = 20260420;
	private const int VarietySeed = 771133;
	private const int TitleSeed = 5150;
	private const int GridSize = 64;

	private const string CommandAuthorityScene = "res://scenes/command_authority.tscn";
	private const string DockyardScene = "res://scenes/dockyard.tscn";
	private const string MapScene = "res://scenes/map.tscn";

	private SubViewport _viewport = null!;
	private SubViewport _battleViewport = null!;
	private TextureRect _viewportDisplay = null!;
	private TextureRect _battleDisplay = null!;
	private ColorRect _transition = null!;
	private Label _titleText = null!;

	/// <summary>
	/// The content currently on screen in the world viewport. Only one piece is
	/// live at a time: the map or a facility. The dissolve helpers cross-fade
	/// from it.
	/// </summary>
	private Node? _current;

	private BattleController? _battle;
	private Task? _battleSimulation;
	private readonly List<BattleController> _retiredBattles = [];
	private CancellationTokenSource? _battleCancel;
	private readonly HashSet<string> _knownUnits = new(StringComparer.Ordinal);
	private double _elapsed;
	private bool _revealArmed;
	private bool _revealTriggered;

	public override void _Ready()
	{
		_viewport = GetNode<SubViewport>("ViewPort");
		_viewport.OwnWorld3D = true;
		_viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;

		_battleViewport = GetNode<SubViewport>("BattleViewPort");
		_battleViewport.OwnWorld3D = true;
		_battleViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;

		// A recording has no player. Without this, a stray click in the game
		// window queues a move and the film loses control of its own battle.
		_viewport.SetDisableInput(true);
		_battleViewport.SetDisableInput(true);

		// The battle is the bottom layer and the world is painted over it, so a
		// dissolve between the two reveals the battle instead of a black frame.
		_battleDisplay = new TextureRect
		{
			Texture = _battleViewport.GetTexture(),
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			AnchorsPreset = (int)Control.LayoutPreset.FullRect,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		AddChild(_battleDisplay);
		MoveChild(_battleDisplay, 0);

		_viewportDisplay = new TextureRect
		{
			Texture = _viewport.GetTexture(),
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			AnchorsPreset = (int)Control.LayoutPreset.FullRect,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		AddChild(_viewportDisplay);
		MoveChild(_viewportDisplay, 1);

		// The film opens on the fight, so the world layer starts hidden.
		_viewportDisplay.Modulate = new Color(1, 1, 1, 0);

		_transition = GetNode<ColorRect>("Overlay/Transition");
		_titleText = GetNode<Label>("Overlay/TitleText");

		// The recording must not carry the game's own tutorials or the text log:
		// the teaser is a systems film, and the teaching overlays are words. The
		// setting is written before the run is created, so the run itself is
		// born without the tutorial flow.
		DisableTeachingOverlays();

		if (Session.Instance.Run is null)
		{
			Session.Instance.StartNewRun();
		}

		// The run is created with tutorials on; the film turns them off.
		Session.Instance.Run?.ConfigureTutorials(false);

		// The frame-rate readout is a development overlay; it does not belong in a
		// recorded video.
		Session.ShowPerformanceOverlay = false;

		RunTeaser();
	}

	// ---- the story ----------------------------------------------------------

	private async void RunTeaser()
	{
		try
		{
			await FadeIn(OpeningFadeSeconds);

			GD.Print($"[Teaser] beat plan at {_elapsed:F2}s");
			await RunPlanBeat();
			GD.Print($"[Teaser] beat execute at {_elapsed:F2}s");
			await RunExecuteBeat();
			GD.Print($"[Teaser] beat variety at {_elapsed:F2}s");
			await RunVarietyBeat();
			GD.Print($"[Teaser] beat star at {_elapsed:F2}s");
			await RunStarBeat();
			GD.Print($"[Teaser] beat contract at {_elapsed:F2}s");
			await RunContractBeat();
			GD.Print($"[Teaser] beat dock at {_elapsed:F2}s");
			await RunDockBeat();
			GD.Print($"[Teaser] beat fleet at {_elapsed:F2}s");
			await RunFleetBeat();
			GD.Print($"[Teaser] beat title at {_elapsed:F2}s");
			await RunTitleBeat();
		}
		catch (Exception error)
		{
			GD.PushError($"[Teaser] Shot list failed: {error}");
		}

		StopBattle();
		await ReleaseRetiredBattles();

		// One single fade to black at the very end.
		await FadeOut(EndFadeSeconds);
		GetTree().Quit();
	}

	/// <summary>
	/// The tactical console. The player side is a real player agent here, so the
	/// battle HUD is live: the turn order at the top, the action cards and the
	/// command wing at the bottom, the pilot's portrait and shields at the
	/// left. The console is put into the cannon's mode, so the frame shows the
	/// weapon's coverage drawn on the space the ship can reach.
	/// </summary>
	private async Task RunPlanBeat()
	{
		await StartBattle(BuildDuelEncounter(
			PlanSeed,
			EType.Fighter,
			EType.Carrier,
			playerIsUser: true,
			secondEnemy: EType.IndustrialGooper));

		FrameActiveShip(PlanDistance, PlanYaw, PlanPitch, PlanRoll);

		// Put the console into the cannon's mode without firing it: the card
		// lights up and the weapon's coverage is drawn in the world.
		await WaitSeconds(0.6f);
		_battle?.RequestAbilityMode(EPlayerMode.LightningCannon, fireImmediately: false);

		await WaitSeconds(PlanSeconds);
	}

	/// <summary>
	/// The order resolves. The scripted player order is a move toward the
	/// enemy and a shot: the hull accelerates away with its trail, the weapon
	/// lands, and a hull is lost.
	/// </summary>
	private async Task RunExecuteBeat()
	{
		// The camera watches the hull that is about to be hit, so the loss
		// happens at the camera rather than at the edge of the frame.
		var targetId = FindTargetUnitId();
		if (targetId is not null)
		{
			_battle?.FocusUnitCamera(targetId, ExecuteDistance, ExecuteYaw, ExecutePitch, ExecuteRoll);
		}
		else
		{
			FrameActiveShip(ExecuteDistance, ExecuteYaw, ExecutePitch, ExecuteRoll);
		}

		_revealArmed = true;

		ScriptPlayerOrder();
		await WaitSeconds(ExecuteSeconds);
	}

	/// <summary>
	/// A second engagement, deliberately unlike the first: another chassis, a
	/// different weapon, a different field and a different camera. The gooper
	/// does not shoot beams; it fouls the space around it.
	/// </summary>
	private async Task RunVarietyBeat()
	{
		await StartBattle(BuildDuelEncounter(
			VarietySeed,
			EType.IndustrialGooper,
			EType.Carrier,
			playerIsUser: true,
			secondEnemy: null));

		FrameActiveShip(VarietyDistance, VarietyYaw, VarietyPitch, VarietyRoll);
		_revealArmed = true;

		if (await WaitForPlayerPlanningAsync(4.5f))
		{
			ScriptPlayerOrder(preferGoop: true);
		}

		await WaitSeconds(VarietySeconds);
	}

	/// <summary>
	/// Waits until the player side is planning, so an order can be scripted for
	/// it. A battle needs a moment before its player agent is ready.
	/// </summary>
	private async Task<bool> WaitForPlayerPlanningAsync(float timeoutSeconds)
	{
		var waited = 0f;
		while (waited < timeoutSeconds)
		{
			if (_battle?.Battle.Phase == EBattlePhase.PlayerTurn
				&& _battle.Battle.PlayerAgent is UserExecutionAgent { IsPlanning: true })
			{
				return true;
			}

			await WaitSeconds(0.05f);
			waited += 0.05f;
		}

		GD.Print("[Teaser] the player agent never started planning");
		return false;
	}

	/// <summary>
	/// The run layer: the star map, with the fleet travelling a route.
	/// </summary>
	private async Task RunStarBeat()
	{
		MapNavigationContext.ClearReturnToFacade();

		var mapInstance = GD.Load<PackedScene>(MapScene).Instantiate();
		await CutToWorld(mapInstance);

		await ToSignal(GetTree(), "process_frame");

		// The new star shader stays in the game; it is not in this film.
		HideMapStar(mapInstance);
		// The map's sky carries a black hole on its horizon. It is a big bright
		// object that reads as a sun, and this film does not have one.
		HideMapEventHorizon(mapInstance);
		// The opening narrative is a page of text. This film has no text.
		DismissMapNarrative(mapInstance);

		PushInOnMap(mapInstance);
		StartFleetTravel();

		// A contract is taken on the map, so the objectives panel fills in and
		// the run shows progress.
		var contractId = PrepareContractOffer();
		if (contractId is not null)
		{
			AcceptContract(contractId);
		}

		await WaitSeconds(StarSeconds);
	}

	/// <summary>
	/// The contract board: the authority's offer list, then the acceptance.
	/// </summary>
	private async Task RunContractBeat()
	{
		var contractId = PrepareContractOffer();
		if (contractId is null)
		{
			GD.Print("[Teaser] no contract available - skipping the contract beat");
			return;
		}

		var (poiId, facilityId) = FindFacility(CommandAuthorityScene);
		GrantContractRole(poiId, facilityId, contractId);
		MapNavigationContext.EnterFacility(poiId, facilityId);

		var authority = GD.Load<PackedScene>(CommandAuthorityScene).Instantiate();
		await DissolveToWorld(authority);

		await ToSignal(GetTree(), "process_frame");

		// The operator button is the real entry point into the contract list.
		if (authority.GetNodeOrNull<BaseButton>("Scene/Manager") is { } manager)
		{
			manager.EmitSignal(BaseButton.SignalName.Pressed);
		}
		else
		{
			GD.Print("[Teaser] command authority has no Manager button");
		}

		await ToSignal(GetTree(), "process_frame");
		await ToSignal(GetTree(), "process_frame");

		// Accept the contract through the run's own action lane, so the game
		// reacts exactly as it does when the player presses Accept.
		AcceptContract(contractId);

		await WaitSeconds(ContractSeconds);
	}

	/// <summary>
	/// Puts the authority's manager on contract duty, so the offer list opens
	/// when the operator is used.
	/// </summary>
	private static void GrantContractRole(string poiId, string facilityId, string contractId)
	{
		var orchestrator = Session.Instance.Run?.StarSystem;
		if (orchestrator is null)
			return;

		var map = orchestrator.Map;
		if (!map.TryGetPointOfInterest(poiId, out var poi))
			return;

		var facility = poi.GetFacility(facilityId);
		foreach (var facilityOperator in facility.Operators)
		{
			// Only the authority's contract giver is put on contract duty. The
			// other operators in the room have roles of their own, and taking
			// those away would change the game.
			if (facilityOperator.SceneSlotId != "Manager")
				continue;

			// The contract board puts an operator on contract duty by itself. If
			// it has already done so, the film does not need to.
			if (poi.OperatorTemporaryRoles.TryGetAssignment(
					facilityId,
					facilityOperator.Name,
					out var assignment)
				&& assignment.Role == EFacilityOperatorRole.Contracts)
			{
				continue;
			}

			poi.OperatorTemporaryRoles.Grant(
				facilityId,
				facilityOperator.Name,
				new OperatorInteraction(EFacilityOperatorRole.Contracts),
				EOperatorAssignmentPriority.Critical,
				contractId,
				orchestrator.Tick + 100000);
		}
	}

	/// <summary>
	/// The dockyard: a hull upgrade on offer, and the purchase that takes it.
	/// </summary>
	private async Task RunDockBeat()
	{
		GrantDockyardMerchantRole();
		GrantResources();

		// The shop only sells upgrade packages once the run has moved up a tier,
		// so the run is moved up a tier before the shop is opened.
		var orchestrator = Session.Instance.Run?.StarSystem;
		for (var i = 0; i < 3; i++)
		{
			orchestrator?.TryBumpDebugDangerProgression();
		}

		var (poiId, facilityId) = FindFacility(DockyardScene);
		MapNavigationContext.EnterFacility(poiId, facilityId);

		var dockyard = GD.Load<PackedScene>(DockyardScene).Instantiate();
		await DissolveToWorld(dockyard);

		await ToSignal(GetTree(), "process_frame");

		if (dockyard.GetNodeOrNull<BaseButton>("Scene/Salesman") is { } salesman)
		{
			salesman.EmitSignal(BaseButton.SignalName.Pressed);
		}
		else
		{
			GD.Print("[Teaser] dockyard has no Salesman button");
		}

		await ToSignal(GetTree(), "process_frame");
		await ToSignal(GetTree(), "process_frame");

		await WaitSeconds(DockSeconds);
	}

	/// <summary>
	/// The fleet and its pilot: the screens the player lives in between fights.
	/// </summary>
	private async Task RunFleetBeat()
	{
		MapNavigationContext.ClearReturnToFacade();

		var mapInstance = GD.Load<PackedScene>(MapScene).Instantiate();
		await CutToWorld(mapInstance);

		await ToSignal(GetTree(), "process_frame");
		await ToSignal(GetTree(), "process_frame");

		HideMapStar(mapInstance);
		HideMapEventHorizon(mapInstance);
		DismissMapNarrative(mapInstance);
		PushInOnMap(mapInstance);

		if (mapInstance.GetNodeOrNull<StrategicHud>("StrategicHud") is { } hud)
		{
			hud.OpenFleetManagement();
		}
		else
		{
			GD.Print("[Teaser] map has no StrategicHud");
		}

		await ToSignal(GetTree(), "process_frame");
		await ToSignal(GetTree(), "process_frame");

		await WaitSeconds(FleetSeconds);
	}

	/// <summary>The closing wordmark, over a fight that is still running.</summary>
	private async Task RunTitleBeat()
	{
		// A fresh engagement for the closing card: an ended battle would show its
		// outcome panel over the name.
		await StartBattle(BuildDuelEncounter(
			TitleSeed,
			EType.Carrier,
			EType.Fighter,
			playerIsUser: true,
			secondEnemy: EType.IndustrialGooper));

		FrameActiveShip(TitleDistance, TitleYaw, TitlePitch, TitleRoll);
		await CutToBattle();

		LayoutTitleText();
		_titleText.Visible = true;

		await ToSignal(GetTree(), "process_frame");
		await WaitSeconds(TitleSeconds);
	}

	// ---- the battle ---------------------------------------------------------

	/// <summary>
	/// Puts a fresh encounter in the battle viewport and starts it. Every battle
	/// in the film is a live encounter built from the current game code.
	/// </summary>
	private async Task StartBattle(BattleEncounter encounter)
	{
		StopBattle();

		BattleController.NextEncounterOverride = encounter;
		BattleController.StaticCameraOverride = true;

		var battleInstance = GD.Load<PackedScene>("res://scenes/battle.tscn").Instantiate();
		_battleViewport.GetNode<Node>("CurrentScene").AddChild(battleInstance);
		_battle = battleInstance.GetNode<BattleController>(".");

		// The battle's own red dwarf is a background element that is enormous
		// next to the map; it bleeds through the world layer, so it is not in
		// this film.
		foreach (var sun in battleInstance
			.FindChildren("RedDwarfSun", "Node3D", true, false)
			.OfType<Node3D>())
		{
			HideTree(sun);
		}

		_knownUnits.Clear();
		_revealArmed = false;
		_revealTriggered = false;

		await ToSignal(GetTree(), "process_frame");

		foreach (var id in _battle.BattleView.UnitViews.Keys)
		{
			_knownUnits.Add(id);
		}

		_battleSimulation = RunBattleAsync();
	}

	/// <summary>
	/// The battle clock. An engagement whose player side is a real player agent
	/// is driven by the teaser script; one whose player side is an AI is driven
	/// by the battle itself.
	/// </summary>
	private async Task RunBattleAsync()
	{
		await ToSignal(GetTree(), "process_frame");

		if (_battle?.Battle.PlayerAgent is not null)
		{
			// A player agent waits for the script; the enemies resolve themselves.
			return;
		}

		_battleCancel = new CancellationTokenSource();
		await _battle!.RunAiVsAiAsync(
			maxTurns: 8,
			onTurnLog: (turn, msg) => GD.Print($"[Teaser Battle] Turn {turn}: {msg}"),
			cancellationToken: _battleCancel.Token);
	}

	/// <summary>
	/// Scripts the player's order for the execution beats: a move toward the
	/// enemy and a shot, committed through the player's own agent so the game
	/// resolves it exactly as it would a human order.
	/// </summary>
	/// <param name="preferGoop">Picks the goop gun over the beam when both are legal.</param>
	private void ScriptPlayerOrder(bool preferGoop = false)
	{
		var battle = _battle?.Battle;
		if (battle is null)
			return;

		if (battle.PlayerAgent is not UserExecutionAgent agent)
		{
			GD.Print("[Teaser] no player agent to script");
			return;
		}

		if (!agent.IsPlanning)
		{
			GD.Print("[Teaser] player agent is not planning yet");
			return;
		}

		var enemyPosition = NearestEnemyPosition(battle, battle.PlayerId);
		if (enemyPosition is null)
		{
			GD.Print("[Teaser] no enemy to move toward");
			return;
		}

		var preview = new PlanningPreview();
		var options = preview.MoveOptions(
			agent.Sim,
			battle.PlayerId,
			battle.PlayerId,
			agent.IsPlanning);

		// Walk as far toward the enemy as the hull can, so the shot shows a
		// ship that has repositioned itself rather than one that fired in place.
		var approach = options
			.Where(option => option.Steps.Count > 0)
			.OrderBy(option => DistanceTo(option.EndPosition, enemyPosition.Value))
			.FirstOrDefault();

		if (approach is not null)
		{
			agent.TryEnqueue(approach.Steps.Cast<IAction>().ToList());
			GD.Print(
				$"[Teaser] scripted move to {approach.EndPosition} " +
				$"({approach.Steps.Count} steps)");
		}

		// Then the weapon, chosen from the ones that are legal right now.
		var legal = Capabilities.LegalCapabilities(agent.Sim, battle.PlayerId);
		var shot = preferGoop
			? legal.FirstOrDefault(action => action is GoopGunAction)
				?? legal.FirstOrDefault(action => action is ScrapDroneSwarmAction)
				?? legal.FirstOrDefault(action => action is LightningCannonAction)
				?? legal.FirstOrDefault(action => action is VoidBombAction)
			: legal.FirstOrDefault(action => action is LightningCannonAction)
				?? legal.FirstOrDefault(action => action is ScrapDroneSwarmAction)
				?? legal.FirstOrDefault(action => action is GoopGunAction)
				?? legal.FirstOrDefault(action => action is VoidBombAction);

		if (shot is not null)
		{
			agent.TryEnqueue([shot]);
			GD.Print($"[Teaser] scripted shot {shot.GetType().Name}");
		}

		battle.EndTurn();
	}

	/// <summary>The position of the enemy hull closest to the given actor.</summary>
	private static Coord? NearestEnemyPosition(BattleOrchestrator battle, string actorId)
	{
		var registry = UnitRegistry.For(battle.Engine.World);
		if (!registry.TryGet(actorId, out var actor))
			return null;

		Coord? best = null;
		var bestDistance = int.MaxValue;
		foreach (var unit in registry.All)
		{
			if (unit.Team != ETeam.Enemy || !unit.State.IsAlive)
				continue;

			var distance = DistanceTo(actor.State.Position, unit.State.Position);
			if (distance >= bestDistance)
				continue;

			bestDistance = distance;
			best = unit.State.Position;
		}

		return best;
	}

	/// <summary>The id of the enemy hull closest to the player.</summary>
	private string? FindTargetUnitId()
	{
		var battle = _battle?.Battle;
		if (battle is null)
			return null;

		var registry = UnitRegistry.For(battle.Engine.World);
		if (!registry.TryGet(battle.PlayerId, out var actor))
			return null;

		string? bestId = null;
		var bestDistance = int.MaxValue;
		foreach (var unit in registry.All)
		{
			if (unit.Team != ETeam.Enemy || !unit.State.IsAlive)
				continue;

			var distance = DistanceTo(actor.State.Position, unit.State.Position);
			if (distance >= bestDistance)
				continue;

			bestDistance = distance;
			bestId = unit.State.Id;
		}

		return bestId;
	}

	private static int DistanceTo(Coord a, Coord b) =>
		System.Math.Abs(a.X - b.X) + System.Math.Abs(a.Y - b.Y) + System.Math.Abs(a.Z - b.Z);

	/// <summary>
	/// Points the camera at the ship the battle is currently acting with.
	/// </summary>
	private void FrameActiveShip(float distance, float yaw, float pitch, float roll)
	{
		if (_battle is null)
			return;

		var subjectId = _battle.Battle.PlayerId;
		if (subjectId is null || !_battle.BattleView.UnitViews.ContainsKey(subjectId))
		{
			subjectId = _battle.BattleView.UnitViews.Keys.FirstOrDefault();
		}

		if (subjectId is null)
			return;

		_battle.FocusUnitCamera(subjectId, distance, yaw, pitch, roll);
	}

	/// <summary>
	/// Drives the battle camera. The pull back is not on a timer: it waits for a
	/// hull to be lost and then retreats, so the move has a cause.
	/// </summary>
	private void UpdateBattleCamera()
	{
		if (_battle is null)
			return;

		var views = _battle.BattleView.UnitViews;

		// A view that disappears is something that was destroyed - a ship, or a
		// void bomb going off. Both are reasons to pull back.
		var lost = false;
		foreach (var id in _knownUnits)
		{
			if (!views.ContainsKey(id))
			{
				lost = true;
				break;
			}
		}

		_knownUnits.Clear();
		foreach (var id in views.Keys)
		{
			_knownUnits.Add(id);
		}

		if (!lost || !_revealArmed)
			return;

		// Something in the fight was just destroyed. Retreat, and tip the horizon
		// the other way so the move reads as the world turning.
		_revealArmed = false;
		_revealTriggered = true;
		GD.Print($"[Teaser] reveal fired at {_elapsed:F2}s");
		_battle.SetupOverviewCamera(
			startDistance: ExecuteDistance,
			dollySeconds: RevealDollySeconds,
			roll: RevealRoll,
			orbitAmplitude: RevealOrbitAmplitude,
			distance: RevealDistance);
	}

	/// <summary>
	/// Ends the current engagement: the replay is stopped, the AI clock is
	/// cancelled and the battle scene is taken out of the viewport, so the next
	/// battle starts from a clean field. The scene is kept alive until the
	/// film ends, because the replay's own timers still fire after the stop and
	/// they must not touch a freed node.
	/// </summary>
	private void StopBattle()
	{
		if (_battleCancel is not null)
		{
			_battleCancel.Cancel();
			_battleCancel.Dispose();
			_battleCancel = null;
		}

		_battleSimulation = null;

		if (_battle is null)
			return;

		var battle = _battle;
		_battle = null;

		battle.StopReplay();
		battle.GetParent()?.RemoveChild(battle);
		_retiredBattles.Add(battle);
	}

	/// <summary>Frees the battles the film has finished with.</summary>
	private async Task ReleaseRetiredBattles()
	{
		foreach (var battle in _retiredBattles)
		{
			if (GodotObject.IsInstanceValid(battle))
				battle.QueueFree();
		}

		_retiredBattles.Clear();
		await ToSignal(GetTree(), "process_frame");
	}

	// ---- encounters ---------------------------------------------------------

	/// <summary>
	/// Builds a live encounter: one hull against one hull, with an optional
	/// second enemy, placed across the middle of the field with a real asteroid
	/// field around them.
	/// </summary>
	private static BattleEncounter BuildDuelEncounter(
		int seed,
		EType playerChassis,
		EType enemyChassis,
		bool playerIsUser,
		EType? secondEnemy = null)
	{
		var (playerSpawn, enemySpawn) = DeploymentPlacement.DevDuel(
			playerChassis,
			enemyChassis,
			seed,
			GridSize,
			$"player-{seed}",
			$"enemy-{seed}");

		var center = new Coord(GridSize / 2, GridSize / 2, GridSize / 2);
		var playerPosition = center - new Coord(2, 0, 0);
		var enemyPosition = center + new Coord(2, 0, 0);

		var spawns = new List<BattleSpawn>
		{
			new()
			{
				Ship = playerSpawn.Ship,
				Pilot = playerSpawn.Pilot,
				Team = ETeam.Player,
				Position = playerPosition,
				Fore = new Coord(1, 0, 0),
				Dorsal = Coord.Up,
				ExecutionAgent = playerIsUser
					? new UserExecutionAgent()
					: new AiExecutionAgent(),
			},
			new()
			{
				Ship = enemySpawn.Ship,
				Pilot = enemySpawn.Pilot,
				Team = ETeam.Enemy,
				Position = enemyPosition,
				Fore = new Coord(-1, 0, 0),
				Dorsal = Coord.Up,
				ExecutionAgent = new AiController(),
			},
		};

		if (secondEnemy is { } second)
		{
			spawns.Add(new BattleSpawn
			{
				Ship = DeploymentPlacement.UpgradeDevShip(
					ShipInstance.FromCatalog($"enemy-b-{seed}", second)),
				Team = ETeam.Enemy,
				Position = enemyPosition + new Coord(0, 2, 0),
				Fore = new Coord(-1, 0, 0),
				Dorsal = Coord.Up,
				ExecutionAgent = new AiController(),
			});
		}

		return new BattleEncounter
		{
			Id = TypedIdGenerator.NextId("teaser-engagement"),
			Seed = seed,
			Spawns = spawns,
			Objective = EObjective.EliminateOpponents,
			WorldHazards = AsteroidFieldGenerator.Generate(new AsteroidFieldConfig
			{
				Seed = seed,
				GridSize = GridSize,
				UnitPositions = spawns.Select(spawn => spawn.Position).ToArray(),
				RegionCenter = center,
				RegionHalfExtent = GridSize / 2 - 2,
				RegionMargin = 2,
				ReservedCells = AsteroidFieldReservations.PlayerCorridors(spawns),
			}),
		};
	}

	// ---- the run layer ------------------------------------------------------

	/// <summary>
	/// Sends the player's fleet along a route. The map draws the course and the
	/// fleet marker travels it, which is the run layer moving.
	/// </summary>
	private static void StartFleetTravel()
	{
		var orchestrator = Session.Instance.Run?.StarSystem;
		if (orchestrator is null)
			return;

		orchestrator.SetRunning();
		orchestrator.RefreshPlayerAgent();

		var agent = orchestrator.PlayerAgent;
		if (agent is null)
			return;

		var map = orchestrator.Map;
		var fleet = map.FleetRegistry.FleetOf(RunState.PlayerFleetUnitId);
		var origin = fleet.State.PositionAt(
			map,
			orchestrator.RuntimeFor(RunState.PlayerFleetUnitId).CachedPath,
			0f).Position;

		// The fleet travels to a dock: the map's own places are obstacles to the
		// route finder, and a dock is the open cell beside one.
		var destinations = map.DocksByPoiId.Values
			.Select(dock => dock.Position)
			.OrderBy(destination => DistanceTo(origin, destination))
			.ToList();

		foreach (var destination in destinations)
		{
			var result = agent.TryQueueMove(destination);
			GD.Print(
				$"[Teaser] fleet travel to {destination} from {origin}: {result.GetType().Name}");
			if (result is CourseCommandResult.Queued)
				return;
		}

		GD.Print("[Teaser] no point of interest was reachable for the fleet");
	}

	/// <summary>
	/// Advances the run until the contract board has an offer, and returns the
	/// contract id.
	/// </summary>
	private static string? PrepareContractOffer()
	{
		var orchestrator = Session.Instance.Run?.StarSystem;
		if (orchestrator is null)
			return null;

		orchestrator.SetRunning();
		for (var i = 0; i < 40 && orchestrator.Map.ContractRegistry.CountPending() == 0; i++)
		{
			orchestrator.AdvanceTicks(2);
		}

		var contractId = orchestrator.Map.ContractRegistry.Pending
			.FirstOrDefault(contract => contract.Category == EContractCategory.Standard)
			?.Id;

		GD.Print($"[Teaser] contract offer: {contractId ?? "none"}");
		return contractId;
	}

	/// <summary>Accepts a contract through the run's own action lane.</summary>
	private static void AcceptContract(string contractId)
	{
		var orchestrator = Session.Instance.Run?.StarSystem;
		if (orchestrator is null)
			return;

		var accepted = orchestrator.TryCommitPlayerInput(
			new AcceptContractAction(RunState.PlayerFleetUnitId, contractId));
		GD.Print($"[Teaser] contract '{contractId}' accepted: {accepted}");
	}

	/// <summary>
	/// Puts the dockyard's salesman on duty, so the merchant HUD has something
	/// to sell.
	/// </summary>
	private static void GrantDockyardMerchantRole()
	{
		var orchestrator = Session.Instance.Run?.StarSystem;
		if (orchestrator is null)
			return;

		var (poiId, facilityId) = FindFacility(DockyardScene);
		var map = orchestrator.Map;
		if (!map.TryGetPointOfInterest(poiId, out var poi))
			return;

		var facility = poi.GetFacility(facilityId);
		foreach (var facilityOperator in facility.Operators)
		{
			// Only the shopkeeper is put on duty. The rest of the dockyard has
			// its own roles.
			if (facilityOperator.SceneSlotId != "Salesman")
				continue;

			poi.OperatorTemporaryRoles.Grant(
				facilityId,
				facilityOperator.Name,
				new OperatorInteraction(EFacilityOperatorRole.Merchant, EMerchantSubtype.Weapons),
				EOperatorAssignmentPriority.Critical,
				"teaser-dockyard",
				orchestrator.Tick + 100000);
		}
	}

	/// <summary>Credits the pilot so the dockyard has something affordable.</summary>
	private static void GrantResources()
	{
		var orchestrator = Session.Instance.Run?.StarSystem;
		orchestrator?.TryGrantDebugResources(ResourceBundle.Create(
			(ResourceId.Credits, 1000),
			(ResourceId.ScrapAlloy, 1000),
			(ResourceId.IndustrialCore, 1000)));
	}

	/// <summary>Finds the POI and facility that own a scene.</summary>
	private static (string PoiId, string FacilityId) FindFacility(string scenePath)
	{
		var map = Session.Instance.Run?.StarSystem?.Map;
		if (map is null)
			throw new InvalidOperationException("No map to search for facilities.");

		foreach (var poi in map.PointsOfInterest)
		{
			foreach (var facility in poi.Facilities)
			{
				if (facility.ScenePath == scenePath)
					return (poi.Id, facility.Id);
			}
		}

		throw new InvalidOperationException($"No facility owns the scene '{scenePath}'.");
	}

	// ---- transitions --------------------------------------------------------

	/// <summary>
	/// Cross-fades the world layer in over the battle, or between two world
	/// shots. The battle stays underneath, so the frame never passes through
	/// black.
	/// </summary>
	private async Task DissolveToWorld(Node incoming)
	{
		var root = _viewport.GetNode<Node>("CurrentScene");
		var outgoing = _current;

		if (incoming is CanvasItem canvasItem)
		{
			// 2D content is drawn in front of 3D content, so fading it up is the
			// dissolve - whether what is behind it is another 2D scene or the map.
			canvasItem.Modulate = new Color(1, 1, 1, 0);
			root.AddChild(incoming);
			_current = incoming;

			_viewportDisplay.Modulate = new Color(1, 1, 1, 1);

			var tween = CreateTween();
			tween.TweenProperty(canvasItem, "modulate:a", 1f, DissolveSeconds)
				.SetTrans(Tween.TransitionType.Sine);
			await ToSignal(tween, "finished");

			FreeStaged(outgoing);
			return;
		}

		// A 3D scene cannot be faded the way 2D content can, so the world layer as
		// a whole is faded over the battle underneath.
		FreeStaged(outgoing);
		root.AddChild(incoming);
		_current = incoming;

		await FadeWorldLayer(1f, DissolveSeconds);
	}

	/// <summary>
	/// A hard cut to a world shot. Used where the film changes gear (the fight
	/// erupting, the sector opening up) and a dissolve would soften the impact.
	/// </summary>
	private async Task CutToWorld(Node incoming)
	{
		var root = _viewport.GetNode<Node>("CurrentScene");
		var outgoing = _current;

		FreeStaged(outgoing);
		root.AddChild(incoming);
		_current = incoming;

		_viewportDisplay.Modulate = new Color(1, 1, 1, 1);
		_battleDisplay.Modulate = new Color(1, 1, 1, 1);
		await ToSignal(GetTree(), "process_frame");
	}

	/// <summary>
	/// A hard cut back to the fight. The world content is dropped and the battle
	/// viewport - which never stopped running - is revealed.
	/// </summary>
	private async Task CutToBattle()
	{
		var root = _viewport.GetNode<Node>("CurrentScene");
		foreach (var child in root.GetChildren())
		{
			root.RemoveChild(child);
			child.QueueFree();
		}

		_current = null;
		_viewportDisplay.Modulate = new Color(1, 1, 1, 0);
		_battleDisplay.Modulate = new Color(1, 1, 1, 1);
		await ToSignal(GetTree(), "process_frame");
	}

	/// <summary>Fades the world layer's alpha, revealing the battle underneath.</summary>
	private async Task FadeWorldLayer(float alpha, float seconds)
	{
		var tween = CreateTween();
		tween.TweenProperty(_viewportDisplay, "modulate:a", alpha, seconds)
			.SetTrans(Tween.TransitionType.Sine);
		await ToSignal(tween, "finished");
	}

	/// <summary>
	/// Pushes the strategic map out and tips the horizon, so the establishing
	/// shot is a place and not a diagram: the routes, the stations and the
	/// fleet all have to fit.
	/// </summary>
	private void PushInOnMap(Node mapInstance)
	{
		if (mapInstance.GetNodeOrNull<MapCamera>("CameraPivot/SpringArm3D/Camera3D") is not { } camera)
			return;

		camera.Roll = 0.09f;
		var pose = camera.CurrentPose;
		camera.TweenToPose(
			new OrbitPose
			{
				Pivot = pose.Pivot,
				Yaw = pose.Yaw + 0.55f,
				Pitch = Mathf.Max(pose.Pitch - 0.04f, 0.18f),
				Distance = pose.Distance * 1.45f,
			},
			2.2f);
	}

	/// <summary>
	/// Hides the map's star. The new star shader is in the game and in the
	/// repository; it is not in this film.
	/// </summary>
	private static void HideMapStar(Node mapInstance)
	{
		foreach (var marker in mapInstance
			.FindChildren("Marker_star", "Node3D", true, false)
			.OfType<Node3D>())
		{
			HideTree(marker);
		}
	}

	/// <summary>Hides a node and everything under it.</summary>
	private static void HideTree(Node node)
	{
		switch (node)
		{
			case VisualInstance3D visual:
				visual.Visible = false;
				break;
			case Node3D node3D:
				node3D.Visible = false;
				break;
			case CanvasItem canvas:
				canvas.Visible = false;
				break;
		}

		foreach (var child in node.GetChildren())
		{
			HideTree(child);
		}
	}

	/// <summary>
	/// Turns off the event horizon in the map's sky. The map paints a black
	/// hole on the horizon, and it is the brightest thing on the map; this film
	/// has no big bright sun-like object in it.
	/// </summary>
	private static void HideMapEventHorizon(Node mapInstance)
	{
		foreach (var environment in mapInstance
			.FindChildren("*", "WorldEnvironment", true, false)
			.OfType<WorldEnvironment>())
		{
			if (environment.Environment?.Sky?.SkyMaterial is ShaderMaterial material)
			{
				material.SetShaderParameter("horizon_strength", 0.0f);
			}
		}
	}

	/// <summary>
	/// Closes the narrative page. The run's opening story is a box of words, and
	/// this film has no words in it. The narrative is also completed in the run
	/// itself, so it cannot come back on a later map.
	/// </summary>
	private static void DismissMapNarrative(Node mapInstance)
	{
		foreach (var narrative in mapInstance
			.FindChildren("*", "CanvasLayer", true, false)
			.OfType<NarrativeHudOverlay>())
		{
			narrative.Close();
		}

		var orchestrator = Session.Instance.Run?.StarSystem;
		var narrativeId = orchestrator?.Map.ActiveNarrativeId;
		if (orchestrator is null || narrativeId is null)
			return;

		orchestrator.PlayerAgent?.TryEnqueue(
			[new CompleteNarrativeAction(RunState.PlayerFleetUnitId, narrativeId)]);
	}

	// ---- helpers ------------------------------------------------------------

	/// <summary>Removes a node from the world viewport.</summary>
	private void FreeStaged(Node? node)
	{
		if (node is null || !GodotObject.IsInstanceValid(node))
			return;

		var root = _viewport.GetNode<Node>("CurrentScene");
		if (node.GetParent() == root)
			root.RemoveChild(node);
		node.QueueFree();
	}

	/// <summary>
	/// Turns the game's own teaching overlays off for the recording: the
	/// tutorial dialog and the action log are words, and this film has none.
	/// </summary>
	private static void DisableTeachingOverlays()
	{
		// The session read the old values when it started; the recording needs
		// the new ones now.
		GameSettings.ApplyShowActionLog(false);

		try
		{
			GameSettings.SaveAll(
				GameSettings.ReadVideoConfig(),
				GameSettings.ReadAudioConfig(),
				showTutorials: false,
				mouseCameraPanEnabled: GameSettings.ReadMouseCameraPan(),
				showActionLog: false,
				GameSettings.ReadKeyBindings());
		}
		catch (Exception error)
		{
			GD.PushError($"[Teaser] could not write the settings file: {error}");
		}
	}

	private async Task WaitSeconds(float seconds) =>
		await ToSignal(GetTree().CreateTimer(seconds), "timeout");

	private async Task FadeIn(float duration)
	{
		_transition.Visible = true;
		_transition.Modulate = new Color(0, 0, 0, 1);
		var tween = GetTree().CreateTween();
		tween.TweenProperty(_transition, "modulate:a", 0.0f, duration);
		tween.SetEase(Tween.EaseType.Out);
		tween.SetTrans(Tween.TransitionType.Quad);
		await ToSignal(tween, "finished");
		_transition.Visible = false;
	}

	private async Task FadeOut(float duration)
	{
		_transition.Visible = true;
		_transition.Modulate = new Color(0, 0, 0, 0);
		var tween = GetTree().CreateTween();
		tween.TweenProperty(_transition, "modulate:a", 1.0f, duration);
		tween.SetEase(Tween.EaseType.In);
		tween.SetTrans(Tween.TransitionType.Quad);
		await ToSignal(tween, "finished");
	}

	/// <summary>
	/// Places the wordmark in the middle of the frame. It is the only text the
	/// film puts on screen, and it has to read over a moving battle.
	/// </summary>
	private void LayoutTitleText()
	{
		_titleText.AnchorLeft = 0f;
		_titleText.AnchorRight = 1f;
		_titleText.AnchorTop = 0.5f;
		_titleText.AnchorBottom = 0.5f;
		_titleText.OffsetLeft = 0f;
		_titleText.OffsetRight = 0f;
		_titleText.OffsetTop = -190f;
		_titleText.OffsetBottom = 190f;
		_titleText.HorizontalAlignment = HorizontalAlignment.Center;
		_titleText.VerticalAlignment = VerticalAlignment.Center;
		_titleText.LabelSettings = new LabelSettings
		{
			FontSize = 118,
			FontColor = new Color(0.97f, 0.98f, 1f),
			OutlineSize = 16,
			OutlineColor = new Color(0f, 0f, 0f, 0.92f),
			ShadowSize = 10,
			ShadowColor = new Color(0f, 0f, 0f, 0.75f),
		};
	}

	public override void _Process(double delta)
	{
		_elapsed += delta;
		UpdateBattleCamera();
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using GrimSpace.Application;
using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Presentation.Scene;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Camera;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Contact;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Presentation.Camera;
using GrimSpace.World.StarSystem.Presentation.Scene;
using GrimSpace.World.StarSystem.Presentation.Ui;
using GrimSpace.World.StarSystem.Resources;
using RunState = GrimSpace.Run.State;
using MapType = GrimSpace.World.StarSystem.Units.EType;

namespace GrimSpace.Presentation.Teaser;

/// <summary>
/// Directs the teaser video (V5 pass: the turn loop is the spine).
///
/// The film is one idea: **a turn-based game, played in front of you.** The
/// battle is not an action scene that happens to have a HUD; it is a loop, and
/// the loop is the story:
///
///   YOUR TURN   the command console is live and lit: the turn order, the
///               action cards, the AP/MP meters, the pilot's face and shields
///   YOU ACT    the weapon is chosen on the console, the order is committed
///               with the game's own EXECUTE control, the hull accelerates away
///               and the shot lands
///   THE BOOM   a hull is lost; the camera retreats because a hull was lost
///   THEIR TURN the console goes dark, the turn order hands over, the enemy
///               acts on the field
///   YOUR TURN  the console comes back on, the round counter has moved, the
///               order strip is full and the cursor is back on you
///   THE RUN    the star map, the contract board, the dockyard, the fleet
///   THE NAME   the name, over a game that is still waiting for you
///
/// Nothing in the film is a still, a mock-up or a staged scene: the battle is
/// the run's own engagement, built by the game's own engagement factory from
/// the run's own fleets, and the world beats are the real map and the real
/// facility scenes.
///
/// Transitions follow one rule: **inside a place the camera flows to whatever
/// is acting; between places it is a clean cut on the event.** No dissolves,
/// no fades to black in the middle of the film, no black frames.
/// </summary>
public partial class TeaserController : Node
{
	// ---- the cut ------------------------------------------------------------
	private const float PlanSeconds = 3.0f;
	private const float YouActSeconds = 1.8f;
	private const float BoomSeconds = 1.4f;
	private const float TheirTurnSeconds = 1.8f;
	private const float YourTurnAgainSeconds = 1.4f;
	private const float StarSeconds = 3.4f;
	private const float ContractSeconds = 3.2f;
	private const float DockSeconds = 3.2f;
	private const float FleetSeconds = 2.8f;
	private const float TitleSeconds = 3.4f;
	private const float DissolveSeconds = 0.4f;
	private const float OpeningFadeSeconds = 0.35f;

	/// <summary>How long the pointer rests on a control before it clicks.</summary>
	private const float HoverDwellSeconds = 0.7f;

	/// <summary>How long the battle may take before the film moves on.</summary>
	private const float BattleBudgetSeconds = 15f;
	private const float EndFadeSeconds = 1.0f;

	// The player's ship, close enough to be the subject.
	private const float PlayerDistance = 9f;
	private const float PlayerYaw = 0.20f;
	private const float PlayerPitch = 0.18f;
	private const float PlayerRoll = -0.12f;

	// The enemy, framed from the other side and a little further out.
	private const float EnemyDistance = 12f;
	private const float EnemyYaw = -0.65f;
	private const float EnemyPitch = 0.26f;
	private const float EnemyRoll = 0.10f;

	// The closing card.
	private const float TitleDistance = 15f;
	private const float TitleYaw = 0.42f;
	private const float TitlePitch = 0.26f;
	private const float TitleRoll = 0.04f;

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
	/// live at a time: the map or a facility.
	/// </summary>
	private Node? _current;

	private BattleController? _battle;
	private Task? _battleSimulation;
	private readonly List<BattleController> _retiredBattles = [];
	private CancellationTokenSource? _battleCancel;

	/// <summary>True while the turn order says the enemy is acting.</summary>
	private bool _enemyActing;

	/// <summary>The hulls that were on the field the last time it was checked.</summary>
	private readonly HashSet<string> _knownUnits = new(StringComparer.Ordinal);

	/// <summary>Where each hull was, the last time it was checked.</summary>
	private readonly Dictionary<string, Vector3> _lastPositions = new(StringComparer.Ordinal);

	/// <summary>
	/// The cursor the viewer sees. The recording cannot capture the operating
	/// system's pointer, so the film draws one - and every move and click it
	/// makes is a real input event pushed into the game's own viewport, so the
	/// hover states, the tooltips and the presses the viewer sees are the
	/// game's own response to a real interaction.
	/// </summary>
	private Node2D? _cursor;

	/// <summary>Arms the pull-back that follows a lost hull.</summary>
	private bool _revealArmed;

	/// <summary>The clock the film runs on.</summary>
	private double _elapsed;

	/// <summary>When the pull-back happens, after the blast has been seen.</summary>
	private double _revealAt;

	public override void _Ready()
	{
		_viewport = GetNode<SubViewport>("ViewPort");
		_viewport.OwnWorld3D = true;
		_viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;

		_battleViewport = GetNode<SubViewport>("BattleViewPort");
		_battleViewport.OwnWorld3D = true;
		_battleViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;

		// A recording has no player. Without this, a stray click in the game
		// window queues an order and the film loses control of its own battle.
		_viewport.SetDisableInput(true);
		_battleViewport.SetDisableInput(true);

		// The battle is the bottom layer and the world is painted over it, so a
		// cut between the two reveals one over the other.
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

		// The film runs the game inside its own viewport, so the session must
		// not take the scene away when the run's battle becomes ready.
		Session.Instance.SuppressSceneChanges = true;

		// The pointer the viewer sees. Every move and click it makes is a real
		// input event for the game.
		BuildCursor();

		RunTeaser();
	}

	// ---- the film -----------------------------------------------------------

	private async void RunTeaser()
	{
		try
		{
			// The run has to be set up before the film starts: the battle in the
			// film is the run's own engagement, produced by the game's own
			// engagement factory from the run's own fleets.
			var engagement = await PrepareRealEngagement();

			await FadeIn(OpeningFadeSeconds);

			await RunTurnLoopBeat(engagement);
			await RunStarBeat();
			await RunContractBeat();
			await RunDockBeat();
			await RunFleetBeat();
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
	/// The turn loop, in five beats, inside one battle. The console lights up
	/// (your turn), the order is committed with the game's own EXECUTE control,
	/// the console goes dark (their turn), it comes back on (your turn again,
	/// one round later) and the second order kills a hull.
	///
	/// The rhythm follows the research: the planning shot is held long enough to
	/// read the board, the payoff is cut fast, and the player's and the
	/// enemy's turns are interleaved rather than blocked together.
	/// </summary>
	private async Task RunTurnLoopBeat(BattleEncounter? engagement)
	{
		await StartBattle(engagement);
		var beatStart = _elapsed;

		// 1. YOUR TURN. The console is lit and the plan is live. This is the
		// longest shot in the film: the board has to be readable.
		await WaitForPlayerTurnAsync(8.0f);
		FrameActingShip(PlayerDistance, PlayerYaw, PlayerPitch, PlayerRoll);
		await WaitSeconds(1.2f);

		// The player picks a weapon on the console. The cursor goes to the card,
		// the card lights up under it, the pointer dwells so the viewer can
		// read what is about to happen, and then the card is clicked. The
		// click is a real button event, so the weapon is queued exactly as a
		// player's click queues it.
		await WaitSeconds(0.4f);
		await ChooseWeaponWithCursor();
		await WaitSeconds(PlanSeconds - 1.7f);

		// 2. YOU ACT. The order is committed by pressing the game's own EXECUTE
		// control, and the hull accelerates away with its trail. The payoff is
		// cut fast.
		await CommitOrderWithCursor();
		_revealArmed = true;
		await WaitSeconds(YouActSeconds);

		// 3. THEIR TURN. The camera goes to the enemy: the turn order has handed
		// over and the console is dark while the enemy acts.
		await WaitSeconds(1.6f);
		FrameUnit(FindEnemyUnitId(), EnemyDistance, EnemyYaw, EnemyPitch, EnemyRoll);
		await WaitSeconds(TheirTurnSeconds);

		// 4. YOUR TURN AGAIN. The console comes back on, the round counter has
		// moved, and the turn strip is full.
		await WaitForPlayerTurnAsync(3.0f);
		FrameUnit(_battle?.Battle.PlayerId, PlayerDistance, PlayerYaw, PlayerPitch, PlayerRoll);
		await WaitSeconds(YourTurnAgainSeconds);

		// 5. YOU ACT AGAIN, and the order that kills a hull: the boom. The camera
		// is on the hull that is about to die, so the loss happens at the camera.
		// The fight is real, so the film plays the exchange again if the first
		// one did not kill anything.
		FrameUnit(FindEnemyUnitId(), EnemyDistance, EnemyYaw, EnemyPitch, EnemyRoll);

		// The fight is real, so the film plays the exchange again if the first
		// one did not kill anything - but the beat has a budget, because the
		// run layer still has to happen.
		var budget = beatStart + BattleBudgetSeconds;
		for (var exchange = 0;
			exchange < 3 && !HullLost && _elapsed < budget;
			exchange++)
		{
			await ChooseWeaponWithCursor();
			await WaitSeconds(0.3f);
			await CommitOrderWithCursor();
			await WaitForHullLostAsync(2.5f);
		}

		await WaitSeconds(BoomSeconds);
	}

	/// <summary>The star map: the fleet, its route, and the run's objectives.</summary>
	private async Task RunStarBeat()
	{
		// The run's engagement is over for the film. The map scene hands the
		// fight over by changing the scene, and the film runs the map inside a
		// viewport of its own, so the run puts the engagement down first.
		Session.Instance.Run!.ActiveBattle = null;

		MapNavigationContext.ClearReturnToFacade();

		var mapInstance = GD.Load<PackedScene>(MapScene).Instantiate();
		await CutToWorld(mapInstance);

		await ToSignal(GetTree(), "process_frame");

		// The new star shader stays in the game; it is not in this film.
		HideMapStar(mapInstance);
		HideMapEventHorizon(mapInstance);
		// The opening narrative is a page of text. This film has no text.
		DismissMapNarrative(mapInstance);

		PushInOnMap(mapInstance);
		StartFleetTravel();

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
		await CutToWorld(authority);

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
	/// The dockyard: a hull upgrade on offer, with real prices.
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
		await CutToWorld(dockyard);

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

	/// <summary>The closing wordmark, over the battle that is still waiting.</summary>
	private async Task RunTitleBeat()
	{
		await CutToBattle();

		LayoutTitleText();
		_titleText.Visible = true;

		await ToSignal(GetTree(), "process_frame");
		await WaitSeconds(TitleSeconds);
	}

	// ---- the real engagement ------------------------------------------------

	/// <summary>
	/// Produces the battle the film fights in, the way the game produces it:
	/// a contract is offered by the real board, the contract is accepted, the
	/// enemy fleet it spawned hunts the player, the fleets make contact, the
	/// player engages, and the run builds the encounter with the game's own
	/// engagement factory. Nothing about the fight is made up.
	/// </summary>
	/// <returns>The run's active engagement, or null if the run produced none.</returns>
	private async Task<BattleEncounter?> PrepareRealEngagement()
	{
		var run = Session.Instance.Run;
		var orchestrator = run?.StarSystem;
		if (run is null || orchestrator is null)
			return null;

		orchestrator.SetRunning();

		// A contract from the real board.
		var contractId = PrepareContractOffer();
		if (contractId is null)
		{
			GD.Print("[Teaser] the contract board produced no offer");
			return null;
		}

		// Accepting it spawns the enemy fleet and starts the hunt.
		AcceptContract(contractId);

		// The fleet the contract spawned is the one the player has to go and meet.
		// This is the game's own order for it.
		var target = FindContractEnemyFleet(orchestrator);
		if (target is not null)
		{
			var queued = orchestrator.PlayerAgent?.TryQueuePursueFleet(target);
			GD.Print($"[Teaser] the fleet pursues {target}: {queued?.GetType().Name ?? "none"}");
		}

		// Let the map run until the fleets make contact.
		var ticks = 0;
		while (ticks < 4000
			&& !EngagementQueries.TryGetPendingPlayerEngagement(
				orchestrator.Map,
				RunState.PlayerFleetUnitId,
				out _))
		{
			orchestrator.AdvanceTicks(5);
			ticks += 5;
		}

		GD.Print($"[Teaser] contact after {ticks} ticks");

		// The contact prompt is the game's; the player presses Engage.
		if (EngagementQueries.TryGetPendingPlayerEngagement(
				orchestrator.Map,
				RunState.PlayerFleetUnitId,
				out _))
		{
			orchestrator.PlayerAgent?.TryEnqueue(
				[new EngageAction(RunState.PlayerFleetUnitId)]);
			orchestrator.AdvanceTicks(4);
		}

		var engagement = run.ActiveBattle;
		GD.Print(
			$"[Teaser] engagement: {engagement?.Id ?? "none"} " +
			$"({engagement?.Spawns.Count ?? 0} hulls)");

		// The map scene hands the fight over by changing the scene, which the
		// teaser cannot allow (the film runs inside a viewport). The run keeps
		// the engagement; the film takes it from here.
		await ToSignal(GetTree(), "process_frame");
		return engagement;
	}

	/// <summary>
	/// The enemy fleet the accepted contract put on the map: the pirate fleet
	/// the player has to fight.
	/// </summary>
	private static string? FindContractEnemyFleet(StarSystemOrchestrator orchestrator)
	{
		string? best = null;
		var bestDistance = double.MaxValue;
		foreach (var fleetId in orchestrator.Map.FleetRegistry.Ids)
		{
			var fleet = orchestrator.Map.FleetRegistry.FleetOf(fleetId);
			if (fleet.State.Type != MapType.PirateFleet)
				continue;

			var position = fleet.State.PositionAt(
				orchestrator.Map,
				orchestrator.RuntimeFor(fleetId).CachedPath,
				0f).Position;
			var player = orchestrator.Map.FleetRegistry
				.FleetOf(RunState.PlayerFleetUnitId)
				.State.PositionAt(
					orchestrator.Map,
					orchestrator.RuntimeFor(RunState.PlayerFleetUnitId).CachedPath,
					0f)
				.Position;
			var distance = System.Math.Abs(position.X - player.X)
				+ System.Math.Abs(position.Z - player.Z);
			if (distance >= bestDistance)
				continue;

			bestDistance = distance;
			best = fleetId;
		}

		return best;
	}

	// ---- the battle ---------------------------------------------------------

	/// <summary>
	/// Starts the battle. With an engagement in hand the battle scene builds it
	/// from the run, exactly as it does when the map hands the fight over.
	/// </summary>
	private async Task StartBattle(BattleEncounter? engagement)
	{
		StopBattle();

		if (engagement is not null)
		{
			// The run's own engagement, through the run's own factory.
			Session.Instance.Run!.ActiveBattle = engagement;
		}

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

		await ToSignal(GetTree(), "process_frame");

		// The hulls are a metre long in a field that is a hundred metres across.
		// A longer lens is the difference between "a dot in a grid" and "a ship".
		if (_battle.GetNodeOrNull<Camera3D>("Camera3D") is { } camera)
		{
			camera.Fov = 58f;
		}

		_battleSimulation = RunBattleAsync();
	}

	/// <summary>
	/// The battle clock. A battle whose player side is a real player agent is
	/// driven by the film; one whose player side is an AI is driven by the
	/// battle itself.
	/// </summary>
	private async Task RunBattleAsync()
	{
		await ToSignal(GetTree(), "process_frame");

		if (_battle?.Battle.PlayerAgent is UserExecutionAgent)
			return;

		_battleCancel = new CancellationTokenSource();
		await _battle!.RunAiVsAiAsync(
			maxTurns: 8,
			onTurnLog: (turn, msg) => GD.Print($"[Teaser Battle] Turn {turn}: {msg}"),
			cancellationToken: _battleCancel.Token);
	}

	/// <summary>
	/// The player picks a weapon on the console. The cursor goes to the card,
	/// the card lights up under it, the pointer dwells on it so the choice is
	/// readable, and then the card is clicked.
	/// </summary>
	private async Task ChooseWeaponWithCursor()
	{
		if (_battle is null)
			return;

		var card = CardFor(EPlayerMode.VoidBomb) ?? CardFor(EPlayerMode.LightningCannon);
		if (card is null)
		{
			ChooseWeapon();
			return;
		}

		var centre = card.GetGlobalRect().GetCenter();
		HoverAt(_battleViewport, centre);
		await WaitSeconds(HoverDwellSeconds);
		ClickAt(_battleViewport, centre);
		GD.Print($"[Teaser] the player clicked the {card.Name} card");
	}

	/// <summary>
	/// The player picks a weapon on the console when there is no card to point
	/// at. The bomb is preferred because its flight path is the most legible
	/// plan in the game: the player can see where the shot will land before it
	/// is committed.
	/// </summary>
	private void ChooseWeapon()
	{
		if (_battle is null)
			return;

		if (_battle.RequestAbilityMode(EPlayerMode.VoidBomb))
		{
			GD.Print("[Teaser] the player chose the bomb on the console");
			return;
		}

		if (_battle.RequestAbilityMode(EPlayerMode.LightningCannon))
			GD.Print("[Teaser] the player chose the cannon on the console");
	}

	/// <summary>
	/// The turn is committed by clicking the console's own EXECUTE control: the
	/// cursor goes there, the button lights up, the pointer dwells, and the
	/// click ends the turn.
	/// </summary>
	private async Task CommitOrderWithCursor()
	{
		if (_battle is null)
			return;

		var execute = _battle.Hud.CommandHud.ExecuteAction.GetChildren()
			.OfType<Button>()
			.FirstOrDefault();
		if (execute is null)
		{
			CommitOrder();
			return;
		}

		var centre = execute.GetGlobalRect().GetCenter();
		HoverAt(_battleViewport, centre);
		await WaitSeconds(HoverDwellSeconds);
		ClickAt(_battleViewport, centre);
		GD.Print("[Teaser] the player clicked EXECUTE");
	}

	/// <summary>
	/// The player commits the turn with the game's own EXECUTE control. The
	/// button is pressed, not a method called, so the game takes exactly the
	/// path a click takes.
	/// </summary>
	private void CommitOrder()
	{
		if (_battle is null)
			return;

		if (_battle.PressExecute())
		{
			GD.Print("[Teaser] the player pressed EXECUTE");
			return;
		}

		GD.Print("[Teaser] no EXECUTE control found");
	}

	/// <summary>Waits until the turn order comes back to the player.</summary>
	private async Task WaitForPlayerTurnAsync(float timeoutSeconds)
	{
		var waited = 0f;
		while (waited < timeoutSeconds)
		{
			if (_battle?.Battle.Phase == EBattlePhase.PlayerTurn)
			{
				_enemyActing = false;
				GD.Print("[Teaser] the player is acting");
				return;
			}

			await WaitSeconds(0.05f);
			waited += 0.05f;
		}

		GD.Print("[Teaser] the player's next turn was not observed");
	}

	/// <summary>
	/// Waits until a hull is lost. This is what ends the boom beat: the pull-back
	/// and the explosion are the game's, and they happen when the simulation
	/// says a hull is gone.
	/// </summary>
	private async Task WaitForHullLostAsync(float timeoutSeconds)
	{
		var waited = 0f;
		while (waited < timeoutSeconds)
		{
			if (HullLost)
			{
				GD.Print($"[Teaser] a hull was lost after {waited:F2}s");
				return;
			}

			await WaitSeconds(0.05f);
			waited += 0.05f;
		}

		GD.Print("[Teaser] no hull was lost inside the beat");
	}

	/// <summary>True once a hull has disappeared from the field.</summary>
	private bool HullLost { get; set; }

	/// <summary>
	/// Watches the field. A hull that disappears was destroyed, and that is the
	/// moment the camera retreats.
	/// </summary>
	private void TrackField()
	{
		if (_battle is null)
			return;

		var views = _battle.BattleView.UnitViews;
		var lost = false;
		Vector3? lostAt = null;
		foreach (var id in _knownUnits)
		{
			if (views.ContainsKey(id))
				continue;

			lost = true;
			if (_lastPositions.TryGetValue(id, out var position))
				lostAt = position;
		}

		_knownUnits.Clear();
		_lastPositions.Clear();
		foreach (var id in views.Keys)
		{
			_knownUnits.Add(id);
			_lastPositions[id] = views[id].GlobalPosition;
		}

		if (_revealAt > 0f && _elapsed >= _revealAt)
		{
			_revealAt = 0f;
			_battle.SetupOverviewCamera(
				startDistance: PlayerDistance,
				dollySeconds: 0.9f,
				roll: 0.11f,
				orbitAmplitude: 0.08f,
				distance: 14f);
		}

		if (!lost || !_revealArmed)
			return;

		// The hull is gone and the explosion is playing. The camera goes to where
		// it happened, so the boom is seen close; the pull-back waits for the
		// blast to have played, then reveals the field.
		_revealArmed = false;
		HullLost = true;
		_revealAt = _elapsed + 1.1f;

		if (lostAt is { } at)
		{
			_battle.FocusPoint(at, EnemyDistance, EnemyYaw, EnemyPitch, EnemyRoll);
			GD.Print($"[Teaser] the camera goes to the loss at {at}");
		}
	}

	/// <summary>True when the given unit id belongs to the enemy side.</summary>
	private bool IsEnemy(string unitId)
	{
		var battle = _battle?.Battle;
		if (battle is null)
			return false;

		return battle.Layout.Participants.TryGetValue(unitId, out var team)
			&& team == ETeam.Enemy;
	}

	/// <summary>
	/// The enemy hull in the battle: the one that is not the player's.
	/// </summary>
	private string? FindEnemyUnitId()
	{
		var battle = _battle?.Battle;
		if (battle is null)
			return null;

		return _battle.BattleView.UnitViews.Keys
			.FirstOrDefault(id => !string.Equals(id, battle.PlayerId, StringComparison.Ordinal));
	}

	/// <summary>Points the camera at one specific hull.</summary>
	private void FrameUnit(string? unitId, float distance, float yaw, float pitch, float roll)
	{
		if (_battle is null || unitId is null)
			return;

		if (!_battle.BattleView.UnitViews.ContainsKey(unitId))
			return;

		_battle.FocusUnitCamera(unitId, distance, yaw, pitch, roll);
		GD.Print($"[Teaser] the camera goes to {unitId}");
	}

	/// <summary>
	/// Points the camera at the ship that is acting. The camera flows to the
	/// subject; it never jumps without a reason. When the turn order says
	/// somebody else is acting, the camera goes to them.
	/// </summary>
	private void FrameActingShip(float distance, float yaw, float pitch, float roll)
	{
		if (_battle is null)
			return;

		var subjectId = _battle.ActiveTurnUnitId;
		if (subjectId is null || !_battle.BattleView.UnitViews.ContainsKey(subjectId))
		{
			subjectId = _battle.Battle.PlayerId;
		}

		if (subjectId is null || !_battle.BattleView.UnitViews.ContainsKey(subjectId))
		{
			subjectId = _battle.BattleView.UnitViews.Keys.FirstOrDefault();
		}

		if (subjectId is null)
			return;

		_battle.FocusUnitCamera(subjectId, distance, yaw, pitch, roll);
		GD.Print($"[Teaser] the camera goes to {subjectId}");
	}

	/// <summary>
	/// Ends the current engagement: the replay is stopped, the AI clock is
	/// cancelled and the battle scene is taken out of the viewport. The scene is
	/// kept alive until the film ends, because the replay's own timers still
	/// fire after the stop and they must not touch a freed node.
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

		// The fleet travels along a route. The map draws the course and the
		// fleet marker travels it, which is the run layer moving. After an
		// engagement the fleet is out at the contact point, so the run's own
		// pursuit order is what it uses to move on.
		var target = FindNearestOtherFleet(orchestrator);
		if (target is not null)
		{
			var queued = agent.TryQueuePursueFleet(target);
			GD.Print($"[Teaser] the fleet moves on {target}: {queued?.GetType().Name ?? "none"}");
		}
	}

	/// <summary>The fleet on the map nearest to the player's own.</summary>
	private static string? FindNearestOtherFleet(StarSystemOrchestrator orchestrator)
	{
		var map = orchestrator.Map;
		var player = map.FleetRegistry.FleetOf(RunState.PlayerFleetUnitId).State.PositionAt(
			map,
			orchestrator.RuntimeFor(RunState.PlayerFleetUnitId).CachedPath,
			0f).Position;

		string? best = null;
		var bestDistance = int.MaxValue;
		foreach (var fleetId in map.FleetRegistry.Ids)
		{
			if (fleetId == RunState.PlayerFleetUnitId)
				continue;

			var position = map.FleetRegistry.FleetOf(fleetId).State.PositionAt(
				map,
				orchestrator.RuntimeFor(fleetId).CachedPath,
				0f).Position;
			var distance = System.Math.Abs(position.X - player.X)
				+ System.Math.Abs(position.Z - player.Z);
			if (distance >= bestDistance)
				continue;

			bestDistance = distance;
			best = fleetId;
		}

		return best;
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
	/// Puts the dockyard's salesman on duty, so the shop has something to sell.
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
	/// A cut to a world shot. The world layer is already rendered, so the cut
	/// lands on a picture, not on a black frame.
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

		// One frame so the new scene has drawn itself before it is shown.
		await ToSignal(GetTree(), "process_frame");
		await ToSignal(GetTree(), "process_frame");
	}

	/// <summary>
	/// A cut back to the fight. The world content is dropped and the battle
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

	// ---- the cursor ---------------------------------------------------------

	/// <summary>
	/// Builds the pointer the film draws. It is drawn, not simulated: the
	/// interaction behind it is real.
	/// </summary>
	private void BuildCursor()
	{
		var root = new Node2D { Name = "Cursor" };
		var outline = new Polygon2D
		{
			Polygon = new Vector2[]
			{
				new(0, 0), new(0, 21), new(5.4f, 16.2f), new(8.8f, 24.2f),
				new(12.2f, 22.6f), new(8.8f, 15.2f), new(14.6f, 15.2f),
			},
			Color = new Color(0.02f, 0.03f, 0.05f, 0.95f),
		};
		var arrow = new Polygon2D
		{
			Polygon = outline.Polygon,
			Color = new Color(0.96f, 0.98f, 1f),
			Scale = new Vector2(0.8f, 0.8f),
		};
		root.AddChild(outline);
		root.AddChild(arrow);
		root.Scale = new Vector2(1.9f, 1.9f);
		GetNode<Node>("Overlay").AddChild(root);
		_cursor = root;
	}

	/// <summary>
	/// Moves the cursor over a point and tells the game about it. The motion is
	/// a real input event, so the game's own hover handling runs.
	/// </summary>
	private void HoverAt(SubViewport target, Vector2 position)
	{
		target.PushInput(new InputEventMouseMotion
		{
			Position = position,
			GlobalPosition = position,
			Relative = Vector2.Zero,
		});

		if (_cursor is not null)
			_cursor.Position = position;
	}

	/// <summary>
	/// Clicks where the cursor is. The click is a real button event, so the
	/// game acts exactly as it does for a player.
	/// </summary>
	private void ClickAt(SubViewport target, Vector2 position)
	{
		HoverAt(target, position);
		target.PushInput(new InputEventMouseButton
		{
			ButtonIndex = MouseButton.Left,
			Pressed = true,
			Position = position,
			GlobalPosition = position,
		});
		target.PushInput(new InputEventMouseButton
		{
			ButtonIndex = MouseButton.Left,
			Pressed = false,
			Position = position,
			GlobalPosition = position,
		});
	}

	/// <summary>The screen position of the console's ability cards.</summary>
	private List<Button> AbilityCards()
	{
		if (_battle is null)
			return [];

		return _battle.Hud.CommandHud.MainActions.GetChildren().OfType<Button>().ToList();
	}

	/// <summary>
	/// The card for a weapon, in the order the console lays them out.
	/// </summary>
	private Button? CardFor(EPlayerMode mode)
	{
		var cards = AbilityCards();
		if (cards.Count == 0 || _battle is null)
			return null;

		var frame = _battle.CurrentFrame;
		if (frame is null)
			return null;

		var specs = AbilityHudCatalog.ForDisplayState(frame.FocusState);
		for (var i = 0; i < specs.Count && i < cards.Count; i++)
		{
			if (specs[i].Mode == mode)
				return cards[i];
		}

		return null;
	}

	// ---- helpers ------------------------------------------------------------

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
	/// Turns off the teaching overlays: the tutorial dialog and the action log
	/// are words, and this film has none.
	/// </summary>
	private static void DisableTeachingOverlays()
	{
		// The session read the old values when it started; the recording needs
		// the new ones now. The mouse edge pan matters most: a cursor sitting at
		// the edge of the window would drag the battle camera off the fight for
		// the whole recording.
		GameSettings.ApplyShowActionLog(false);
		GameSettings.ApplyMouseCameraPan(false);

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

	private static int DistanceTo(Coord a, Coord b) =>
		System.Math.Abs(a.X - b.X) + System.Math.Abs(a.Y - b.Y) + System.Math.Abs(a.Z - b.Z);

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

	public override void _Process(double delta)
	{
		_elapsed += delta;
		TrackField();
	}
}

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
using GrimSpace.Battle.Encounter.Generation;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Presentation.Scene;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Camera;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;
using GrimSpace.Run;
using GrimSpace.Units;
using GrimSpace.Battle.Units;
using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.Units.Maneuvering;
using BattleType = GrimSpace.Units.Enums.EType;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Presentation.Camera;
using GrimSpace.World.StarSystem.Presentation.Scene;
using GrimSpace.World.StarSystem.Presentation.Ui;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Units;
using RunState = GrimSpace.Run.State;
using MapType = GrimSpace.World.StarSystem.Units.EType;
using BattleUnitType = GrimSpace.Units.Enums.EType;
using MapFactory = GrimSpace.World.StarSystem.Units.Factory;

namespace GrimSpace.Presentation.Teaser;

/// <summary>
/// Directs the teaser video (V6 rebuild: events first, the turn loop visible,
/// the run layer paid off).
///
/// The film is a chain of consequences, not a list of screens:
///
///   1  ANCHOR     the whole console, the enemy outnumbering you, and the
///                 enemy's plan already drawn on the field
///   2  COMMIT     your order resolves on camera: the hull accelerates away and
///                 the beam crosses the frame
///   3  THE LOSS   a hull is destroyed, and the wreck is still there after
///   4  THE PLAN   the next turn's threat is drawn again, and the round moves on
///   5  THE GOOP   a different enemy, a different weapon, a green field of
///                 hazard
///   6  THE MAP   the route the fleet is travelling, drawn on the map
///   7  THE BOARD  the authority, and the contract that will pay for it
///   8  THE SHOP  what the money buys
///   9  THE CREW   the ship and the pilot, and the objective that is live
///  10  THE CARRIER another fight, a bigger ship, the beams again
///   11  THE DEBT  the enemy plan, unanswered: the promise the film ends on
///   12  THE CARD  the name, a promise and a call
///
/// Every frame is pixels from a real `.tscn` scene. The three engagements are
/// built by the game's own engagement factory from fleets registered in the
/// run's own inventory; what the film stages is the roster and the order, not
/// the look.
///
/// There is no pointer in this film. The interaction is shown by its
/// response: the card lights, the order resolves, the hull moves, the impact
/// lands.
/// </summary>
public partial class TeaserController : Node
{
	// ---- the cut ------------------------------------------------------------
	// The schedule. Every beat ends on its mark, so the card lands where the
	// end-card window says it should: 0:30.70.
	private const float OpeningFadeSeconds = 0.33f;
	private const float OfferEnd = 1.60f;
	private const float RouteEnd = 3.00f;
	private const float AnchorEnd = 4.90f;
	private const float CommitEnd = 6.20f;
	private const float LossEnd = 11.60f;
	private const float PlanEnd = 13.00f;
	private const float GoopEnd = 14.20f;
	private const float GoopFieldEnd = 15.60f;
	private const float MapEnd = 17.00f;
	private const float ShopEnd = 18.40f;
	private const float CarrierEnd = 21.80f;
	private const float CarrierWideEnd = 24.60f;
	private const float ReachEnd = 27.40f;
	private const float DebtEnd = 30.70f;
	private const float TitleEnd = 34.60f;

	// The card has to land inside the industry end-card window and hold.
	private const float TitleOnset = 30.70f;

	// ---- camera -------------------------------------------------------------
	private const float PlayerDistance = 9.5f;

	/// <summary>The goop beat is filmed close: the hazard is a small patch.</summary>
	private const float GoopDistance = 7f;
	private const float PlayerYaw = 0.20f;
	private const float PlayerPitch = 0.20f;
	private const float PlayerRoll = -0.10f;

	private const float EnemyDistance = 12f;
	private const float EnemyYaw = -0.55f;
	private const float EnemyPitch = 0.24f;
	private const float EnemyRoll = 0.10f;

	private const float WideDistance = 30f;
	private const float WideYaw = 0.42f;
	private const float WidePitch = 0.46f;
	private const float WideRoll = 0.04f;

	private const string CommandAuthorityScene = "res://scenes/command_authority.tscn";
	private const string DockyardScene = "res://scenes/dockyard.tscn";
	private const string MapScene = "res://scenes/map.tscn";

	private SubViewport _viewport = null!;
	private SubViewport _battleViewport = null!;
	private TextureRect _viewportDisplay = null!;
	private TextureRect _battleDisplay = null!;
	private ColorRect _transition = null!;
	private Label _titleText = null!;
	private Label _promiseText = null!;
	private Label _callText = null!;

	private Node? _current;

	private BattleController? _battle;
	private Task? _battleSimulation;
	private readonly List<BattleController> _retiredBattles = [];
	private readonly List<CancellationTokenSource> _retiredCancels = [];
	private string? _acceptedContractId;
	private CancellationTokenSource? _battleCancel;

	private readonly HashSet<string> _knownUnits = new(StringComparer.Ordinal);
	private readonly Dictionary<string, Vector3> _lastPositions = new(StringComparer.Ordinal);
	private bool _revealArmed;
	private double _revealAt;
	private double _elapsed;

	/// <summary>The engagement ids the film stages, one per battle.</summary>
	private const int AnchorSeed = 9110;
	private const int GoopSeed = 4470;
	private const int CarrierSeed = 7730;

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

		_viewportDisplay.Modulate = new Color(1, 1, 1, 0);

		_transition = GetNode<ColorRect>("Overlay/Transition");
		_titleText = GetNode<Label>("Overlay/TitleText");
		BuildTitleCard();

		// The recording must not carry the game's own tutorials or the text log.
		DisableTeachingOverlays();

		if (Session.Instance.Run is null)
		{
			Session.Instance.StartNewRun();
		}

		Session.Instance.Run?.ConfigureTutorials(false);
		Session.ShowPerformanceOverlay = false;

		// The film runs the game inside its own viewport, so the session must not
		// take the scene away when the run's battle becomes ready.
		Session.Instance.SuppressSceneChanges = true;

		RunTeaser();
	}

	// ---- the film -----------------------------------------------------------

	private async void RunTeaser()
	{
		try
		{
			await FadeIn(OpeningFadeSeconds);

			// The goal, in the first twelve per cent: the offer, the route it
			// names, and the ship committing to it.
			await RunOfferBeat();
			await RunRouteBeat();

			// The pursuit, and what it costs.
			await RunAnchorBeat();
			await RunCommitBeat();
			await RunLossBeat();
			await RunPlanBeat();
			await RunGoopBeat();
			await RunGoopFieldBeat();
			await RunMapBeat();
			await RunShopBeat();

			// The last run of it, and the promise.
			await RunCarrierBeat();
			await RunReachBeat();
			await RunThreatBeat();
			await RunTitleBeat();
		}
		catch (Exception error)
		{
			GD.PushError($"[Teaser] Shot list failed: {error}");
		}

		StopBattle();
		await ToSignal(GetTree(), "process_frame");
		await ToSignal(GetTree(), "process_frame");
		GetTree().Quit();
	}

	/// <summary>
	/// The anchor: the whole console, the enemy outnumbering you, and the
	/// enemy's plan already drawn on the field. The HUD is at full size here
	/// and nowhere else.
	/// </summary>
	private async Task RunAnchorBeat()
	{
		await StartBattle(BuildStagedEncounter(
			AnchorSeed,
			"teaser-anchor",
			[BattleType.Fighter],
			canonicalLayout: true));

		// The run layer is dropped: the fight takes the screen.
		await CutToBattle();

		// The wide: the enemy force is visibly larger than yours.
		_battle?.SetupOverviewCamera(
			startDistance: WideDistance + 12f,
			dollySeconds: 1.6f,
			roll: WideRoll,
			orbitAmplitude: 0.12f,
			distance: WideDistance);

		await HoldUntil(AnchorEnd);

		// The console belongs in the shot that tells the viewer what the game
		// is. Everywhere after it, the picture is the game.
		_battle?.Hud.SetChromeVisible(false);
	}

	/// <summary>
	/// Your order resolves on camera: the hull accelerates away and the beam
	/// crosses the frame. The card lights before the order is committed, so the
	/// selection is visible without a pointer.
	/// </summary>
	private async Task RunCommitBeat()
	{
		FrameActingShip(PlayerDistance, PlayerYaw, PlayerPitch, PlayerRoll);
		await WaitSeconds(0.35f);

		// The selection state: the card lights and the weapon's coverage is
		// drawn on the field.
		_battle?.RequestAbilityMode(EPlayerMode.LightningCannon, fireImmediately: false);
		await WaitSeconds(0.45f);

		// The order goes in, through the game's own agent.
		CommitOrder();
		await HoldUntil(CommitEnd);
	}

	/// <summary>
	/// A hull is destroyed. The camera goes to where the loss happened, and the
	/// wreck is still on screen when the film leaves the battle.
	/// </summary>
	private async Task RunLossBeat()
	{
		// The camera sits on the hull that is going to be lost, so the loss
		// happens at the camera and the wreck is still there when the film
		// leaves the battle.
		FrameUnit(_battle?.Battle.PlayerId, PlayerDistance, PlayerYaw, PlayerPitch, PlayerRoll);
		_revealArmed = true;

		// The fight is real, so the film plays the exchange again until a hull
		// is actually destroyed.
		var deadline = _elapsed + 6.4;
		while (!HullLost && _elapsed < deadline)
		{
			await WaitForHullLostAsync(0.6f);
			if (HullLost)
				break;

			_battle?.RequestAbilityMode(EPlayerMode.LightningCannon, fireImmediately: false);
			CommitOrder();
		}

		await HoldUntil(LossEnd);
	}

	/// <summary>
	/// The next turn: the enemy plan is drawn again on the field, and the round
	/// counter moves on. The telegraph is a red arc and a rising tone.
	/// </summary>
	private async Task RunPlanBeat()
	{
		await WaitForPlayerTurnAsync(1.2f);
		FrameActingShip(EnemyDistance, EnemyYaw, EnemyPitch, EnemyRoll);
		await HoldUntil(PlanEnd);
	}

	/// <summary>
	/// A different engagement: a goop gun turns the space into a green field of
	/// hazard, and the enemy that is shooting at you is a different chassis.
	/// </summary>
	private async Task RunGoopBeat()
	{
		await StartBattle(BuildStagedEncounter(
			GoopSeed,
			"teaser-goop",
			[BattleType.IndustrialGooper, BattleType.Fighter]));

		await CutToBattle();

		FrameUnit(_battle?.Battle.PlayerId, GoopDistance, PlayerYaw, PlayerPitch, PlayerRoll);
		await WaitForPlayerTurnAsync(1.2f);

		_battle?.RequestAbilityMode(EPlayerMode.GoopGun, fireImmediately: false);
		await WaitSeconds(0.35f);
		CommitOrder(EPlayerMode.GoopGun);

		await HoldUntil(GoopEnd);
	}

	/// <summary>
	/// The goop field: the hazard spreads through the space the ships fight in,
	/// and the fighter has to fight inside it.
	/// </summary>
	private async Task RunGoopFieldBeat()
	{
		// The field is in front of the hull that fired it, so the camera stays
		// on the shooter and the hazard fills the frame.
		FrameUnit(_battle?.Battle.PlayerId, GoopDistance, PlayerYaw, PlayerPitch, PlayerRoll);
		await HoldUntil(GoopFieldEnd);
	}

	/// <summary>
	/// The run layer: the map, with the route the fleet is travelling drawn on
	/// it. The map is lit for the film; the new sun stays out of it.
	/// </summary>
	private async Task RunMapBeat()
	{
		// The engagement is over for the film; the map scene hands the fight
		// over by changing the scene, and the film runs the map inside a
		// viewport of its own.
		Session.Instance.Run!.ActiveBattle = null;

		MapNavigationContext.ClearReturnToFacade();

		var mapInstance = GD.Load<PackedScene>(MapScene).Instantiate();
		await CutToWorld(mapInstance);

		await ToSignal(GetTree(), "process_frame");

		HideMapStar(mapInstance);
		HideMapEventHorizon(mapInstance);
		DismissMapNarrative(mapInstance);
		LightMapForFilm(mapInstance);
		PushInOnMap(mapInstance);

		await HoldUntil(MapEnd);
	}

	/// <summary>
	/// The offer: the contract board with the one claim that is on it, and the
	/// acceptance that puts a live objective on the map. This is the film's
	/// first beat, because the film has to say what it is about before it
	/// shows anything else.
	/// </summary>
	private async Task RunOfferBeat()
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

		if (authority.GetNodeOrNull<BaseButton>("Scene/Manager") is { } manager)
		{
			manager.EmitSignal(BaseButton.SignalName.Pressed);
		}

		await ToSignal(GetTree(), "process_frame");
		await ToSignal(GetTree(), "process_frame");

		// Accept it through the run's own action lane, so the game reacts
		// exactly as it does when the player accepts.
		AcceptContract(contractId);
		_acceptedContractId = contractId;

		await HoldUntil(OfferEnd);
	}

	/// <summary>
	/// The goal in reach: the map, the route the ship has run, and the wreck at
	/// the end of it.
	/// </summary>
	private async Task RunReachBeat()
	{
		Session.Instance.Run!.ActiveBattle = null;

		MapNavigationContext.ClearReturnToFacade();

		var mapInstance = GD.Load<PackedScene>(MapScene).Instantiate();
		await CutToWorld(mapInstance);

		await ToSignal(GetTree(), "process_frame");

		HideMapStar(mapInstance);
		HideMapEventHorizon(mapInstance);
		DismissMapNarrative(mapInstance);
		LightMapForFilm(mapInstance);
		PushInOnMap(mapInstance, 1.2f);

		await HoldUntil(ReachEnd);
	}

	/// <summary>
	/// The unpaid threat: the enemy's plan drawn across the field, and no
	/// answer to it. The film ends here, and the card lands on the tension.
	/// </summary>
	private async Task RunThreatBeat()
	{
		await CutToBattle();

		FrameUnit(FindEnemyUnitId(), EnemyDistance, EnemyYaw, EnemyPitch, EnemyRoll);
		await HoldUntil(DebtEnd);
	}

	/// <summary>
	/// The shop: what the money buys. The price rows, for as long as they can
	/// be read.
	/// </summary>
	private async Task RunShopBeat()
	{
		GrantDockyardMerchantRole();
		GrantResources();

		var orchestrator = Session.Instance.Run?.StarSystem;
		for (var i = 0; i < 8; i++)
		{
			orchestrator?.TryBumpDebugDangerProgression();
		}

		var (poiId, facilityId) = FindFacility(DockyardScene);
		MapNavigationContext.EnterFacility(poiId, facilityId);

		var dockyard = GD.Load<PackedScene>(DockyardScene).Instantiate();
		await CutToWorld(dockyard);

		await ToSignal(GetTree(), "process_frame");

		LightWorldForFilm(dockyard);

		await ToSignal(GetTree(), "process_frame");
		await ToSignal(GetTree(), "process_frame");

		await HoldUntil(ShopEnd);
	}

	/// <summary>
	/// The route: the map with the run drawn on it, the objective that is now
	/// live with its badge, and the ship already moving toward the wreck. The
	/// goal is on screen by the third second of the film.
	/// </summary>
	private async Task RunRouteBeat()
	{
		MapNavigationContext.ClearReturnToFacade();

		var mapInstance = GD.Load<PackedScene>(MapScene).Instantiate();
		await CutToWorld(mapInstance);

		await ToSignal(GetTree(), "process_frame");
		await ToSignal(GetTree(), "process_frame");

		HideMapStar(mapInstance);
		HideMapEventHorizon(mapInstance);
		DismissMapNarrative(mapInstance);
		LightMapForFilm(mapInstance);
		PushInOnMap(mapInstance, 0.8f);

		// The contract became live while the authority room was on screen, so
		// the objectives panel never saw it. The panel is the game's own: it is
		// told about the change it missed, and it shows its own badge.
		if (_acceptedContractId is { } contractId
			&& mapInstance.GetNodeOrNull<StrategicHud>("StrategicHud") is { } strategicHud)
		{
			strategicHud.Objectives.NotifyContractStateChanged(contractId, EContractStatus.Active);
		}

		StartRouteToGoal();

		await ToSignal(GetTree(), "process_frame");
		await HoldUntil(RouteEnd);
	}

	/// <summary>
	/// The carrier: a bigger ship, the beams again, and a third palette.
	/// </summary>
	private async Task RunCarrierBeat()
	{
		// The third engagement: a bigger hull, the beams, three enemy hulls
		// against one. The first shot is the shot the player gives.
		await StartBattle(BuildStagedEncounter(
			CarrierSeed,
			"teaser-carrier-a",
			[BattleType.Carrier, BattleType.Fighter, BattleType.IndustrialGooper]));

		// The world layer (the map, the panels) is dropped: from here to the
		// card the film is the fight and nothing else.
		await CutToBattle();

		FrameUnit(FindEnemyUnitId(), EnemyDistance, EnemyYaw, EnemyPitch, EnemyRoll);
		await WaitForPlayerTurnAsync(1.2f);

		_battle?.RequestAbilityMode(EPlayerMode.LightningCannon, fireImmediately: false);
		await WaitSeconds(0.35f);
		CommitOrder();

		await HoldUntil(CarrierEnd);

		// The second shot: a fresh field, the enemy force counted, and the
		// enemy's plan drawn and left unanswered.
		await StartBattle(BuildStagedEncounter(
			CarrierSeed + 1,
			"teaser-carrier-b",
			[BattleType.Carrier, BattleType.Fighter, BattleType.IndustrialGooper]));

		await CutToBattle();

		FrameUnit(FindEnemyUnitId(), WideDistance, WideYaw, WidePitch, WideRoll);
		await WaitForPlayerTurnAsync(1.2f);
		await HoldUntil(CarrierWideEnd);
	}

	/// <summary>
	/// The card: the name, a promise and a call, on a clean plate over the live
	/// fight.
	/// </summary>
	private async Task RunTitleBeat()
	{
		// The fight keeps running behind the card, with the world labels off:
		// nothing may print through the wordmark.
		_battle?.Hud.SetChromeVisible(false);
		HideWorldLabels();

		await CutToBattle();

		LayoutTitleCard();
		_titleText.Visible = true;
		_promiseText.Visible = true;
		_callText.Visible = true;

		await ToSignal(GetTree(), "process_frame");
		await HoldUntil(TitleEnd);
	}

	// ---- the battle ---------------------------------------------------------

	/// <summary>
	/// Builds an engagement with the game's own engagement factory, from fleets
	/// registered in the run's own inventory. The roster is staged; the scene,
	/// the placement, the agents and the simulation are the game's.
	/// </summary>
	private BattleEncounter BuildStagedEncounter(
		int seed,
		string id,
		BattleType[] enemyChassis,
		bool canonicalLayout = false)
	{
		var run = Session.Instance.Run
			?? throw new InvalidOperationException("The film needs a run.");
		var inventory = run.ItemInventory;
		var map = run.StarSystem.Map;

		var playerFleet = map.FleetRegistry.FleetOf(RunState.PlayerFleetUnitId);
		var enemyFleetId = $"teaser-enemy-{seed}";

		// The enemy fleet: real ships, registered in the run's own inventory, so
		// the game's own factory can place them.
		var memberTypes = enemyChassis.Select(chassis => (BattleUnitType)chassis).ToList();
		var enemyFleet = MapFactory.Create(
			new Spawn(
				enemyFleetId,
				MapType.PirateFleet,
				new Coord(64, 64, 64),
				UnitDefaults.SpeedPerTick(MapType.PirateFleet),
				UnitDefaults.EngageRadius(MapType.PirateFleet),
				UnitDefaults.VisionRadius(MapType.PirateFleet),
				[]),
			memberTypes);

		foreach (var declaration in enemyFleet.Registrations)
		{
			// The enemy hulls are upgraded, so one good hit is worth something.
			// They are still the game's own ships, registered in the run's own
			// inventory, and the game's own factory places them.
			inventory.Register(
				DeploymentPlacement.UpgradeDevShip(
					ShipInstance.FromCatalog(declaration.ShipId, declaration.Chassis)),
				enemyFleetId);
		}

		// The player's own hull is upgraded the same way.
		foreach (var shipId in playerFleet.Members.Select(member => member.Id))
		{
			if (!inventory.TryGet(shipId, out var ship) || ship is not ShipInstance instance)
				continue;

			inventory.Update(DeploymentPlacement.UpgradeDevShip(instance));
		}

		return EngagementBattleFactory.Create(
			[playerFleet, enemyFleet],
			inventory,
			seed,
			id,
			RunState.PlayerFleetUnitId,
			BuildPilotSnapshots(run),
			canonicalLayout);
	}

	/// <summary>The run's pilots, as the battle factory wants them.</summary>
	private static IReadOnlyDictionary<string, PilotBattleSnapshot> BuildPilotSnapshots(RunState run)
	{
		var snapshots = new Dictionary<string, PilotBattleSnapshot>(StringComparer.Ordinal);
		foreach (var pilot in run.PilotRegistry.Active)
		{
			if (string.IsNullOrEmpty(pilot.AssignedShipId))
				continue;

			snapshots[pilot.AssignedShipId] = new PilotBattleSnapshot(
				pilot.Id,
				PilotSkillAvailability.LearnedBattleSkills(pilot));
		}

		return snapshots;
	}

	/// <summary>
	/// Starts a battle. The encounter is built by the game's own factory; the
	/// scene is the game's own.
	/// </summary>
	private async Task StartBattle(BattleEncounter encounter)
	{
		StopBattle();

		BattleController.NextEncounterOverride = encounter;

		var battleInstance = GD.Load<PackedScene>("res://scenes/battle.tscn").Instantiate();
		_battleViewport.GetNode<Node>("CurrentScene").AddChild(battleInstance);
		_battle = battleInstance.GetNode<BattleController>(".");

		// The space dust is a near-field particle layer. Seen through a long
		// lens it becomes a curtain of bokeh across the whole frame, so the film
		// culls it.
		foreach (var dust in battleInstance
			.FindChildren("FineDust", "MultiMeshInstance3D", true, false)
			.OfType<MultiMeshInstance3D>())
		{
			dust.Visible = false;
		}

		// The battle's own red dwarf is enormous next to the map and bleeds
		// through the world layer.
		foreach (var sun in battleInstance
			.FindChildren("RedDwarfSun", "Node3D", true, false)
			.OfType<Node3D>())
		{
			HideTree(sun);
		}

		// The battle is lit for the recording: the stock exposure leaves most
		// of the frame under the luma floor, and the film's events are the
		// things that should carry the brightness.
		foreach (var environment in battleInstance
			.FindChildren("*", "WorldEnvironment", true, false)
			.OfType<WorldEnvironment>())
		{
			if (environment.Environment is not { } settings)
				continue;

			settings.AmbientLightEnergy = 1.60f;
			settings.AmbientLightColor = new Color(0.38f, 0.42f, 0.54f);
			settings.TonemapExposure = 3.40f;

			// The sky is most of the frame. Lifted, the field reads as space
			// instead of as a black rectangle, and the events have something to
			// happen against.
			if (settings.Sky?.SkyMaterial is PanoramaSkyMaterial sky)
				sky.EnergyMultiplier = 8.0f;
		}

		_knownUnits.Clear();
		_lastPositions.Clear();
		_revealArmed = false;
		HullLost = false;

		await ToSignal(GetTree(), "process_frame");

		foreach (var view in _battle.BattleView.UnitViews)
		{
			_knownUnits.Add(view.Key);
			_lastPositions[view.Key] = view.Value.GlobalPosition;
		}

		_battleSimulation = RunBattleAsync();
	}

	private async Task RunBattleAsync()
	{
		await ToSignal(GetTree(), "process_frame");

		if (_battle?.Battle.PlayerAgent is not null)
			return;

		_battleCancel = new CancellationTokenSource();
		try
		{
			await _battle!.RunAiVsAiAsync(
				maxTurns: 8,
				onTurnLog: (turn, msg) => GD.Print($"[Teaser Battle] Turn {turn}: {msg}"),
				cancellationToken: _battleCancel.Token);
		}
		catch (ObjectDisposedException)
		{
			// The battle was retired mid-turn: its node is gone and the
			// simulation it was running on has been disposed with it.
		}
	}

	/// <summary>
	/// The order goes in through the player's own agent, so the game resolves
	/// it exactly as it resolves a human's.
	/// </summary>
	private void CommitOrder(EPlayerMode preferred = EPlayerMode.LightningCannon)
	{
		var battle = _battle?.Battle;
		if (battle is null)
			return;

		if (battle.PlayerAgent is not UserExecutionAgent agent || !agent.IsPlanning)
			return;

		var legal = Capabilities.LegalCapabilities(agent.Sim, battle.PlayerId);
		var shots = new List<IAction>();
		shots.AddRange(legal.Where(action => Matches(action, preferred)));
		shots.AddRange(legal.Where(action => action is VoidBombAction && !shots.Contains(action)));
		if (shots.Count == 0)
			return;

		// The weapons fire along the hull's nose. The order therefore starts by
		// walking the hull onto the target's line and turning the nose onto it,
		// so the shot is a shot and not a discharge into empty space.
		//
		// Each part is queued on its own: the order is rejected as a whole if
		// one action in it is illegal, and a rejected order is a turn where
		// nothing happens.
		var queued = new List<string>();

		var approach = ApproachTarget(battle, agent);
		if (approach is not null)
		{
			foreach (var step in approach.Value.Steps)
			{
				if (agent.TryEnqueue([step]))
					queued.Add("Move");
			}
		}

		var turn = TurnOntoTarget(
			battle,
			agent,
			approach?.EndPosition,
			approach?.EndFore);
		if (turn is not null && agent.TryEnqueue([new HeadingTurnAction(battle.PlayerId, turn.Value)]))
			queued.Add("Turn");

		foreach (var shot in shots)
		{
			if (agent.TryEnqueue([shot]))
				queued.Add(shot.GetType().Name);
		}

		GD.Print($"[Teaser] the order is committed: {string.Join(", ", queued)}");
		battle.EndTurn();
	}

	/// <summary>
	/// The move that walks the hull onto the target's line, or null when the
	/// hull is already lined up.
	/// </summary>
	private static (IReadOnlyList<IAction> Steps, Coord EndPosition, Coord EndFore)? ApproachTarget(
		BattleOrchestrator battle,
		UserExecutionAgent agent)
	{
		var registry = UnitRegistry.For(battle.Engine.World);
		if (!registry.TryGet(battle.PlayerId, out var player))
			return null;

		Coord? target = null;
		foreach (var pair in battle.Layout.Participants)
		{
			if (pair.Value != ETeam.Enemy)
				continue;

			if (registry.TryGet(pair.Key, out var enemy))
				target = enemy.State.Position;
		}

		if (target is null)
			return null;

		var delta = target.Value - player.State.Position;
		if (System.Math.Abs(delta.X) + System.Math.Abs(delta.Z) <= 2)
			return null;

		var preview = new PlanningPreview();
		var options = preview.MoveOptions(agent.Sim, battle.PlayerId, battle.PlayerId, agent.IsPlanning);

		// The shot only lands along the hull's nose, so the move that matters is
		// the one that lines the hull up with the target, not the one that gets
		// closest to it.
		var aligned = options
			.Where(option => option.Steps.Count > 0)
			.Where(option =>
				option.EndPosition.X == target.Value.X
				|| option.EndPosition.Z == target.Value.Z)
			.OrderBy(option => DistanceTo(option.EndPosition, target.Value))
			.ToList();

		var approach = aligned.FirstOrDefault()
			?? options
				.Where(option => option.Steps.Count > 0)
				.OrderBy(option => DistanceTo(option.EndPosition, target.Value))
				.FirstOrDefault();
		if (approach is null)
			return null;

		return (
			approach.Steps.Cast<IAction>().ToList(),
			approach.EndPosition,
			approach.ResultState.Fore);
	}

	/// <summary>
	/// The turn that puts the hull's nose on the target, or null when it is
	/// already pointing at it.
	/// </summary>
	private static EHeadingTurn? TurnOntoTarget(
		BattleOrchestrator battle,
		UserExecutionAgent agent,
		Coord? from = null,
		Coord? fore = null)
	{
		var targetId = battle.Layout.Participants
			.FirstOrDefault(pair => pair.Value == ETeam.Enemy).Key;
		if (targetId is null)
			return null;

		var registry = UnitRegistry.For(battle.Engine.World);
		if (!registry.TryGet(battle.PlayerId, out var player)
			|| !registry.TryGet(targetId, out var target))
			return null;

		var origin = from ?? player.State.Position;
		var nose = fore ?? player.State.Fore;
		var delta = target.State.Position - origin;
		var toward = System.Math.Abs(delta.X) >= System.Math.Abs(delta.Z)
			? (delta.X > 0 ? Right : Left)
			: (delta.Z > 0 ? Forward : Back);

		return TurnBetween(nose, toward);
	}

	private static readonly Coord[] HeadingOrder =
	[
		new(0, 0, 1),
		new(1, 0, 0),
		new(0, 0, -1),
		new(-1, 0, 0),
	];

	private static EHeadingTurn? TurnBetween(Coord fore, Coord toward)
	{
		var from = Array.IndexOf(HeadingOrder, fore);
		var to = Array.IndexOf(HeadingOrder, toward);
		if (from < 0 || to < 0 || from == to)
			return null;

		var diff = (to - from + 4) % 4;
		return diff switch
		{
			1 => EHeadingTurn.YawRight,
			2 => EHeadingTurn.Yaw180,
			3 => EHeadingTurn.YawLeft,
			_ => null,
		};
	}

	private static int DistanceTo(Coord a, Coord b) =>
		System.Math.Abs(a.X - b.X) + System.Math.Abs(a.Y - b.Y) + System.Math.Abs(a.Z - b.Z);

	private static Coord Forward => new(0, 0, 1);
	private static Coord Right => new(1, 0, 0);
	private static Coord Back => new(0, 0, -1);
	private static Coord Left => new(-1, 0, 0);

	private static void PrintEngagement(BattleOrchestrator battle)
	{
		var registry = UnitRegistry.For(battle.Engine.World);
		foreach (var pair in battle.Layout.Participants)
		{
			if (!registry.TryGet(pair.Key, out var unit))
				continue;

			var shields = unit.State.ShieldPoints;
			var fore = unit.State.Fore;
			GD.Print(
				$"[Teaser]   {pair.Key} team={pair.Value} pos={unit.State.Position} " +
				$"fore={fore} hull={unit.State.HullPoints} shields={shields}");
		}
	}

	private static bool Matches(IAction action, EPlayerMode mode) => mode switch
	{
		EPlayerMode.LightningCannon => action is LightningCannonAction,
		EPlayerMode.GoopGun => action is GoopGunAction,
		EPlayerMode.ScrapDroneSwarm => action is ScrapDroneSwarmAction,
		EPlayerMode.VoidBomb => action is VoidBombAction,
		_ => false,
	};

	/// <summary>The enemy hull in the battle: the one that is not the player's.</summary>
	private string? FindEnemyUnitId()
	{
		var battle = _battle?.Battle;
		if (battle is null)
			return null;

		return _battle.BattleView.UnitViews.Keys
			.FirstOrDefault(id =>
				!id.StartsWith("__", StringComparison.Ordinal)
				&& !string.Equals(id, battle.PlayerId, StringComparison.Ordinal));
	}

	/// <summary>Points the camera at one specific hull.</summary>
	private void FrameUnit(string? unitId, float distance, float yaw, float pitch, float roll)
	{
		if (_battle is null || unitId is null)
			return;

		if (!_battle.BattleView.UnitViews.ContainsKey(unitId))
			return;

		_battle.FocusUnitCamera(unitId, distance, yaw, pitch, roll);
	}

	/// <summary>Points the camera at whatever is acting.</summary>
	private void FrameActingShip(float distance, float yaw, float pitch, float roll)
	{
		var subjectId = _battle?.ActiveTurnUnitId;
		if (subjectId is null || _battle?.BattleView.UnitViews.ContainsKey(subjectId) != true)
		{
			subjectId = _battle?.Battle.PlayerId;
		}

		FrameUnit(subjectId, distance, yaw, pitch, roll);
	}

	private async Task WaitForPlayerTurnAsync(float timeoutSeconds)
	{
		var waited = 0f;
		while (waited < timeoutSeconds)
		{
			if (_battle?.Battle.Phase == EBattlePhase.PlayerTurn)
				return;

			await WaitSeconds(0.05f);
			waited += 0.05f;
		}
	}

	private async Task WaitForHullLostAsync(float timeoutSeconds)
	{
		var waited = 0f;
		while (waited < timeoutSeconds)
		{
			if (HullLost)
				return;

			await WaitSeconds(0.05f);
			waited += 0.05f;
		}
	}

	/// <summary>True once a hull has disappeared from the field.</summary>
	private bool HullLost { get; set; }

	/// <summary>
	/// Watches the field. A hull that disappears was destroyed, and that is the
	/// moment the camera goes to it.
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
				startDistance: EnemyDistance,
				dollySeconds: 0.9f,
				roll: EnemyRoll,
				orbitAmplitude: 0.08f,
				distance: 16f);
		}

		if (!lost || !_revealArmed)
			return;

		_revealArmed = false;
		HullLost = true;
		_revealAt = _elapsed + 1.2f;

		if (lostAt is { } at)
		{
			_battle.FocusPoint(at, EnemyDistance, EnemyYaw, EnemyPitch, EnemyRoll);
			GD.Print($"[Teaser] a hull was lost at {at}");
		}
	}

	private void StopBattle()
	{
		if (_battleCancel is not null)
		{
			// Cancelled, not disposed: the retired battle is still awaiting on
			// this token, and disposing it under that await throws.
			_battleCancel.Cancel();
			_retiredCancels.Add(_battleCancel);
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

	private async Task ReleaseRetiredBattles()
	{
		foreach (var cancel in _retiredCancels)
		{
			cancel.Dispose();
		}

		_retiredCancels.Clear();

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
	/// The route the film is about: the ship runs for the wreck the claim
	/// names. If the claim has no wreck on the map, the ship runs for the
	/// nearest fleet instead, so the map always has a route drawn on it.
	/// </summary>
	private void StartRouteToGoal()
	{
		if (_acceptedContractId is { } contractId)
		{
			var orchestrator = Session.Instance.Run?.StarSystem;
			if (orchestrator is not null)
			{
				orchestrator.SetRunning();
				orchestrator.RefreshPlayerAgent();

				var agent = orchestrator.PlayerAgent;
				if (agent is not null)
				{
					var result = agent.TryQueueWreckContact(contractId);
					GD.Print($"[Teaser] the ship runs for the wreck on {contractId}: "
						+ $"{result.GetType().Name}");
					if (result is CourseCommandResult.Queued)
						return;
				}
			}
		}

		StartFleetTravel();
	}

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

		var target = FindNearestOtherFleet(orchestrator);
		if (target is null)
			return;

		var queued = agent.TryQueuePursueFleet(target);
		GD.Print($"[Teaser] the fleet moves on {target}: {queued?.GetType().Name ?? "none"}");
	}

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

		// The film needs a claim with a wreck on the map: that is the goal the
		// ship can run for, and the thing the card promises.
		var contractId = orchestrator.Map.ContractRegistry.Pending
			.FirstOrDefault(contract => contract.Objective is WreckageObjective)
			?.Id
			?? orchestrator.Map.ContractRegistry.Pending
				.FirstOrDefault(contract => contract.Category == EContractCategory.Standard)
				?.Id;

		GD.Print($"[Teaser] contract offer: {contractId ?? "none"}");
		return contractId;
	}

	private static void AcceptContract(string contractId)
	{
		var orchestrator = Session.Instance.Run?.StarSystem;
		if (orchestrator is null)
			return;

		var accepted = orchestrator.TryCommitPlayerInput(
			new AcceptContractAction(RunState.PlayerFleetUnitId, contractId));
		GD.Print($"[Teaser] contract '{contractId}' accepted: {accepted}");
	}

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
			if (facilityOperator.SceneSlotId != "Manager")
				continue;

			// The operator may already be on contract duty, or on another role.
			// Either way the film wants the contract list, so the old role goes
			// first.
			poi.OperatorTemporaryRoles.RevokeSource(
				facilityId,
				facilityOperator.Name,
				contractId);

			poi.OperatorTemporaryRoles.Grant(
				facilityId,
				facilityOperator.Name,
				new OperatorInteraction(EFacilityOperatorRole.Contracts),
				EOperatorAssignmentPriority.Critical,
				contractId,
				orchestrator.Tick + 100000);
		}
	}

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
			if (facilityOperator.SceneSlotId != "Salesman")
				continue;

			poi.OperatorTemporaryRoles.RevokeSource(
				facilityId,
				facilityOperator.Name,
				"teaser-dockyard");

			poi.OperatorTemporaryRoles.Grant(
				facilityId,
				facilityOperator.Name,
				new OperatorInteraction(EFacilityOperatorRole.Merchant, EMerchantSubtype.Weapons),
				EOperatorAssignmentPriority.Critical,
				"teaser-dockyard",
				orchestrator.Tick + 100000);
		}
	}

	private static void GrantResources()
	{
		var orchestrator = Session.Instance.Run?.StarSystem;
		orchestrator?.TryGrantDebugResources(ResourceBundle.Create(
			(ResourceId.Credits, 1000),
			(ResourceId.ScrapAlloy, 1000),
			(ResourceId.IndustrialCore, 1000)));
	}

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
	/// A cut to a world shot. The world viewport has already rendered the next
	/// scene before the cut, so the cut lands on a picture and never on black.
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

	// ---- the card -----------------------------------------------------------

	/// <summary>
	/// Builds the closing card: the name in the game's own typography, one
	/// promise line and one call.
	/// </summary>
	private void BuildTitleCard()
	{
		var theme = GD.Load<Theme>("res://assets/ui/themes/theatrical_theme.tres");
		var overlay = GetNode<Node>("Overlay");

		// The scrim: the world behind the card is the live field, and the name
		// has to be read over whatever happens to be in frame. It is a soft
		// radial falloff, so it darkens the type without drawing a box.
		var falloff = new Gradient
		{
			Offsets = new[] { 0.0f, 0.55f, 1.0f },
			Colors = new[]
			{
				new Color(0.02f, 0.03f, 0.05f, 0.34f),
				new Color(0.02f, 0.03f, 0.05f, 0.20f),
				new Color(0.02f, 0.03f, 0.05f, 0.0f),
			},
		};

		var scrim = new TextureRect
		{
			Texture = new GradientTexture2D
			{
				Gradient = falloff,
				Fill = GradientTexture2D.FillEnum.Radial,
				FillFrom = new Vector2(0.5f, 0.5f),
				FillTo = new Vector2(1.0f, 0.5f),
				Width = 256,
				Height = 256,
			},
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.Scale,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			AnchorLeft = 0.5f,
			AnchorRight = 0.5f,
			AnchorTop = 0.5f,
			AnchorBottom = 0.5f,
			OffsetLeft = -700f,
			OffsetRight = 700f,
			OffsetTop = -390f,
			OffsetBottom = 240f,
		};
		overlay.AddChild(scrim);
		overlay.MoveChild(scrim, 0);

		_promiseText = new Label
		{
			Text = "RECOVER WHAT WAS LOST. PAY IN HULLS.",
			Theme = theme,
			ThemeTypeVariation = "SubtitleLabel",
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Visible = false,
		};
		overlay.AddChild(_promiseText);

		_callText = new Label
		{
			Text = "WISHLIST ON STEAM",
			Theme = theme,
			ThemeTypeVariation = "SectionHeadingLabel",
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Visible = false,
		};
		overlay.AddChild(_callText);
	}

	/// <summary>Places the card in the middle of the frame, over the fight.</summary>
	private void LayoutTitleCard()
	{
		_titleText.AnchorLeft = 0f;
		_titleText.AnchorRight = 1f;
		_titleText.AnchorTop = 0.5f;
		_titleText.AnchorBottom = 0.5f;
		_titleText.OffsetLeft = 0f;
		_titleText.OffsetRight = 0f;
		_titleText.OffsetTop = -300f;
		_titleText.OffsetBottom = -30f;
		_titleText.HorizontalAlignment = HorizontalAlignment.Center;
		_titleText.VerticalAlignment = VerticalAlignment.Center;
		_titleText.LabelSettings = new LabelSettings
		{
			FontSize = 112,
			FontColor = new Color(0.97f, 0.98f, 1f),
			OutlineSize = 18,
			OutlineColor = new Color(0f, 0f, 0f, 0.95f),
			ShadowSize = 12,
			ShadowColor = new Color(0f, 0f, 0f, 0.8f),
		};

		_promiseText.AnchorLeft = 0f;
		_promiseText.AnchorRight = 1f;
		_promiseText.AnchorTop = 0.5f;
		_promiseText.AnchorBottom = 0.5f;
		_promiseText.OffsetLeft = 0f;
		_promiseText.OffsetRight = 0f;
		_promiseText.OffsetTop = -10f;
		_promiseText.OffsetBottom = 50f;
		_promiseText.LabelSettings = new LabelSettings
		{
			FontSize = 36,
			FontColor = new Color(0.86f, 0.90f, 0.96f),
			OutlineSize = 10,
			OutlineColor = new Color(0f, 0f, 0f, 0.92f),
		};

		_callText.AnchorLeft = 0f;
		_callText.AnchorRight = 1f;
		_callText.AnchorTop = 0.5f;
		_callText.AnchorBottom = 0.5f;
		_callText.OffsetLeft = 0f;
		_callText.OffsetRight = 0f;
		_callText.OffsetTop = 70f;
		_callText.OffsetBottom = 130f;
		_callText.LabelSettings = new LabelSettings
		{
			FontSize = 34,
			FontColor = new Color(0.92f, 0.95f, 1.00f),
			OutlineSize = 8,
			OutlineColor = new Color(0f, 0f, 0f, 0.9f),
		};
	}

	// ---- helpers ------------------------------------------------------------

	private void PushInOnMap(Node mapInstance, float seconds = 2.0f)
	{
		if (mapInstance.GetNodeOrNull<MapCamera>("CameraPivot/SpringArm3D/Camera3D") is not { } camera)
			return;

		camera.Roll = 0.06f;
		var pose = camera.CurrentPose;
		camera.TweenToPose(
			new OrbitPose
			{
				Pivot = pose.Pivot,
				Yaw = pose.Yaw + 0.45f,
				Pitch = Mathf.Max(pose.Pitch - 0.02f, 0.20f),
				Distance = pose.Distance * 0.35f,
			},
			seconds);
	}

	/// <summary>
	/// Lifts the map for the film. The map's own exposure is too dark to read
	/// the route; the sun stays out, so the light has to come from elsewhere.
	/// </summary>
	private static void LightMapForFilm(Node mapInstance)
	{
		foreach (var environment in mapInstance
			.FindChildren("*", "WorldEnvironment", true, false)
			.OfType<WorldEnvironment>())
		{
			if (environment.Environment is not { } settings)
				continue;

			settings.AmbientLightEnergy = 1.80f;
			settings.AmbientLightColor = new Color(0.38f, 0.42f, 0.54f);
			settings.TonemapExposure = 3.60f;

			if (settings.Sky?.SkyMaterial is PanoramaSkyMaterial sky)
				sky.EnergyMultiplier = 8.0f;
		}

		foreach (var light in mapInstance
			.FindChildren("*", "DirectionalLight3D", true, false)
			.OfType<DirectionalLight3D>())
		{
			light.LightEnergy = 2.2f;
		}
	}

	/// <summary>
	/// Lifts a world scene for the recording: the film's frames have to sit
	/// above the luma floor, and the stock lighting leaves them under it.
	/// </summary>
	private static void LightWorldForFilm(Node worldInstance)
	{
		foreach (var environment in worldInstance
			.FindChildren("*", "WorldEnvironment", true, false)
			.OfType<WorldEnvironment>())
		{
			if (environment.Environment is not { } settings)
				continue;

			settings.AmbientLightEnergy = 1.80f;
			settings.AmbientLightColor = new Color(0.38f, 0.42f, 0.54f);
			settings.TonemapExposure = 3.60f;

			if (settings.Sky?.SkyMaterial is PanoramaSkyMaterial sky)
				sky.EnergyMultiplier = 8.0f;
		}

		foreach (var light in worldInstance
			.FindChildren("*", "DirectionalLight3D", true, false)
			.OfType<DirectionalLight3D>())
		{
			light.LightEnergy = 3.2f;
		}

		foreach (var light in worldInstance
			.FindChildren("*", "OmniLight3D", true, false)
			.OfType<OmniLight3D>())
		{
			light.LightEnergy *= 4.0f;
		}

		foreach (var light in worldInstance
			.FindChildren("*", "SpotLight3D", true, false)
			.OfType<SpotLight3D>())
		{
			light.LightEnergy *= 4.0f;
		}
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
	/// hole on the horizon; it is the brightest thing on the map and reads as a
	/// sun.
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
	/// Closes the narrative page, and completes it in the run so it cannot come
	/// back. The run's opening story is a box of words and this film has none.
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

	/// <summary>
	/// Hides the in-world labels (the target reticle and the range tether) so
	/// nothing prints through the closing wordmark.
	/// </summary>
	private void HideWorldLabels()
	{
		if (_battle is null)
			return;

		foreach (var label in _battle.GetNode<Node>(".").FindChildren("*", "CanvasItem", true, false)
			.OfType<CanvasItem>())
		{
			if (label is EnemyTargetInfoOverlay)
				label.Visible = false;
			else if (label.GetType().Name == "EnemyRangeTetherView")
				label.Visible = false;
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

	private async Task WaitSeconds(float seconds) =>
		await ToSignal(GetTree().CreateTimer(seconds), "timeout");

	/// <summary>Holds the beat until its mark, so the cut keeps its rhythm.</summary>
	private async Task HoldUntil(double endTime)
	{
		while (_elapsed < endTime)
		{
			await WaitSeconds(0.02f);
		}
	}

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

	public override void _Process(double delta)
	{
		_elapsed += delta;
		TrackField();
		HideMarkerScrawl();
	}

	/// <summary>
	/// The destruction in this film is the hull breaking up, not a hand-drawn
	/// word on the screen. The game's marker billboard is hidden for the
	/// recording so the wreck reads as the wreck.
	/// </summary>
	private void HideMarkerScrawl()
	{
		if (_battle is null)
			return;

		foreach (var node in _battle.GetNode<Node>(".").FindChildren("ShipDeathBoom", "Node3D", true, false)
			.OfType<Node3D>())
		{
			node.Visible = false;
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using GrimSpace.Application;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Ids;
using GrimSpace.Battle.Presentation.Scene;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Camera;
using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem.Presentation.Camera;
using GrimSpace.World.StarSystem.Presentation.Scene;

namespace GrimSpace.Presentation.Teaser;

/// <summary>
/// Directs the teaser video.
///
/// The cut tells one story, and every shot in it is live game content:
///
///   HOOK    a fight is already under way
///   WORLD   the sector it happens in
///   JOB     the contract that put the pilot there
///   FIGHT   the battle the contract turns into
///   COST    what the battle took
///   STAKES  the galaxy the battle does not fix
///   TITLE   the name
///
/// The battle runs in its own viewport so it can keep playing underneath the
/// world beats; that is what lets the film open on the fight, cut away to the
/// sector, and come back to the same fight later in the story.
///
/// Transitions are hard cuts between the battle and the world (the fight should
/// hit like a cut) and cross-dissolves inside the world. Nothing ever fades to
/// black mid-film: the frame that dissolves into the battle reveals the battle,
/// not a void.
/// </summary>
public partial class TeaserController : Node
{
	// ---- the cut ------------------------------------------------------------
	private const float HookSeconds = 2.6f;
	private const float PilotSeconds = 1.4f;
	private const float WorldSeconds = 4.2f;
	private const float JobSeconds = 2.2f;
	private const float FightSeconds = 5.8f;
	private const float CostSeconds = 3.4f;
	private const float StakesSeconds = 3.0f;
	private const float TitleSeconds = 6.0f;
	private const float DissolveSeconds = 0.4f;
	private const float OpeningFadeSeconds = 0.4f;
	private const float EndFadeSeconds = 1.0f;

	// Hook framing: hull level and tilted hard, so the first frame is a shot and
	// not a board.
	private const float HookCameraDistance = 12f;
	private const float HookCameraYaw = 2.35f;
	private const float HookCameraPitch = 0.20f;
	private const float HookCameraRoll = -0.21f;

	// Fight framing: a step back, so the whole engagement is legible while the
	// ships are still trading fire.
	private const float FightCameraDistance = 19f;
	private const float FightCameraYaw = 1.95f;
	private const float FightCameraPitch = 0.36f;
	private const float FightCameraRoll = -0.13f;

	// The reveal. The camera does not drift away from the fight on a timer: it
	// retreats because a hull was just lost, and the horizon tips the other way
	// so the move reads as a decision instead of a slide.
	private const float RevealRoll = 0.12f;
	private const float RevealDollySeconds = 0.8f;
	private const float RevealOrbitAmplitude = 0.09f;
	private const float RevealDistance = 32f;

	// The strategic establishing shot is tilted as well: a level horizon reads as
	// a map, a tipped one reads as a place.
	private const float MapRoll = 0.09f;
	private const float MapPushSeconds = 2.0f;

	// The paintings get a small tilt of their own so the still shots are not the
	// only square frames in the film.
	private const float StillTiltDegrees = 2.2f;
	private const float StillPush = 1.12f;

	private const int CombatSeed = 99999;
	private const string TravelScenePath = "res://scenes/travel.tscn";
	private const string PilotArt = "res://assets/story/pages/pilot-drunk.png";
	private const string StakesArt = "res://assets/story/pages/total-war.png";
	private const string TitleArt = "res://assets/story/pages/war-and-ruin2.png";

	// Measurement mode: `--teaser-battle` on the command line records the battle
	// on its own, so its timeline can be measured from the pixels.
	private const float BattleMeasurementSeconds = 34f;

	private static bool BattleOnlyMode =>
		OS.GetCmdlineUserArgs().Any(arg => arg == "--teaser-battle");

	private SubViewport _viewport = null!;
	private SubViewport _battleViewport = null!;
	private TextureRect _viewportDisplay = null!;
	private TextureRect _battleDisplay = null!;
	private ColorRect _transition = null!;
	private Label _titleText = null!;
	private Label _subtitleText = null!;
	private Label _phaseLabel = null!;
	private Label _captionText = null!;
	private TextureRect _titleScrim = null!;

	/// <summary>
	/// The content currently on screen in the world viewport. Only one piece is
	/// live at a time: a still, a 2D scene or a 3D scene. The dissolve helpers
	/// cross-fade from it.
	/// </summary>
	private Node? _current;

	private BattleController? _battle;
	private Task? _battleTask;
	private Task? _battleSimulation;
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
		_subtitleText = GetNode<Label>("Overlay/SubtitleText");
		_phaseLabel = GetNode<Label>("Overlay/PhaseLabel");
		BuildCaption();
		BuildTitleScrim();

		if (Session.Instance.Run is null)
		{
			Session.Instance.StartNewRun();
		}

		// The frame-rate readout is a development overlay; it does not belong in a
		// recorded video.
		Session.ShowPerformanceOverlay = false;

		StartBattle();
		RunTeaser();
	}

	// ---- the story ----------------------------------------------------------

	private async void RunTeaser()
	{
		try
		{
			await FadeIn(OpeningFadeSeconds);

			if (BattleOnlyMode)
			{
				// Measurement pass: show the whole battle so its timeline can be
				// measured from the pixels (when the bomb goes off, when a hull is
				// lost) instead of guessed.
				ShowBattle();
				SetTag("TACTICAL");
				SetCaption("MEASUREMENT PASS");
				await WaitSeconds(BattleMeasurementSeconds);
			}
			else
			{
				await RunHookBeat();
				await RunWorldBeat();
				await RunPilotBeat();
				await RunJobBeat();
				await RunFightBeat();
				await RunCostBeat();
				await RunStakesBeat();
				await RunTitleBeat();
			}
		}
		catch (Exception error)
		{
			GD.PushError($"[Teaser] Shot list failed: {error}");
		}

		StopBattle();

		// One single fade to black at the very end. Earlier revisions faded out and
		// then faded back in here, which strobed the last two seconds.
		await FadeOut(EndFadeSeconds);
		GetTree().Quit();
	}

	/// <summary>
	/// The cold open: live tactical combat, close on a hull, tilted. The viewer
	/// lands in the middle of a fight and has to find out what it is.
	/// </summary>
	private async Task RunHookBeat()
	{
		ShowBattle();
		SetTag("TACTICAL");
		SetCaption("ZIMA-ALPHA. SOMETHING IS BLOCKING THE ROUTE.");
		await WaitSeconds(HookSeconds);
	}

	/// <summary>Hides the world layer so the fight underneath is the picture.</summary>
	private void ShowBattle()
	{
		_viewportDisplay.Modulate = new Color(1, 1, 1, 0);
		_battleDisplay.Modulate = new Color(1, 1, 1, 1);
	}

	/// <summary>
	/// The pilot's world: the district he wakes up in. The game's own art for
	/// "Year 4326 - A Pilot in a small sub-sector of the known galaxy: Zima-Alpha,
	/// wakes up from a neuro-drug binge."
	/// </summary>
	private async Task RunPilotBeat()
	{
		// A hard cut, not a dissolve: the map's own dialogue box is HUD, and
		// cross-fading a still under it leaves the box floating over the art.
		await CutToWorld(BuildStills(PilotArt));

		SetTag("YEAR 4326");
		SetCaption("A PILOT, A RUSTBUCKET, AND A SHIFT TO WORK.");

		await ToSignal(GetTree(), "process_frame");
		DriftStill(PilotSeconds);
		await WaitSeconds(PilotSeconds);
	}

	/// <summary>The sector: the strategic map the run is played on.</summary>
	private async Task RunWorldBeat()
	{
		// The travel beat entered a facility, which makes the map open on that POI's
		// facade. The teaser wants the wide strategic view instead.
		MapNavigationContext.ClearReturnToFacade();

		var mapInstance = GD.Load<PackedScene>("res://scenes/map.tscn").Instantiate();
		await CutToWorld(mapInstance);

		SetTag("ZIMA-ALPHA");
		SetCaption("THE ROUTE IS BLOCKED. THE CONTRACT IS NOT.");

		await ToSignal(GetTree(), "process_frame");
		await ToSignal(GetTree(), "process_frame");
		PushInOnMap(mapInstance, MapPushSeconds);
		await WaitSeconds(WorldSeconds);
	}

	/// <summary>The job: a station interior, and the contract that caused the fight.</summary>
	private async Task RunJobBeat()
	{
		// Facility scenes bind to a live POI in the run; without one they render but
		// log an error.
		EnterTravelFacility();

		var travelInstance = GD.Load<PackedScene>(TravelScenePath).Instantiate();
		await DissolveToWorld(travelInstance);

		SetTag("STATION");
		SetCaption(
			"PIRATE ACTIVITY IS REDUCING ROUTE EFFICIENCY. "
			+ "THE OPTIMALITY DECREES THEM AS SCRAP!");

		await ToSignal(GetTree(), "process_frame");
		PushInOnScene(travelInstance, JobSeconds);
		await WaitSeconds(JobSeconds);
	}

	/// <summary>
	/// The fight itself. The battle has been running since the first frame, so
	/// this beat joins it in the middle of the engagement: the camera opens out, a
	/// void bomb goes off, a hull is lost, and the camera retreats to show what it
	/// cost.
	/// </summary>
	private async Task RunFightBeat()
	{
		// The framing changes on the cut, so the shot opens already composed.
		ArmFightFraming();

		await CutToBattle();

		SetTag("TACTICAL");
		SetCaption($"TWO HULLS BLOCK THE ROUTE. YOU HAVE A VOID BOMB.");

		await WaitSeconds(FightSeconds);
	}

	/// <summary>
	/// The cost. The cards are the battle's own roster and the battle's own hull
	/// numbers - whatever the simulation did to the player's fleet is what the
	/// film says it did.
	/// </summary>
	private async Task RunCostBeat()
	{
		var report = BuildDamageReport();
		await CutToWorld(BuildReportScene(report));

		SetTag("FLEET");
		SetCaption(CostCaption(report));

		await ToSignal(GetTree(), "process_frame");
		PushInOnScene(_current!, CostSeconds);
		await WaitSeconds(CostSeconds);
	}

	/// <summary>The world the fight does not fix.</summary>
	private async Task RunStakesBeat()
	{
		await DissolveToWorld(BuildStills(StakesArt));

		SetTag("THE GALAXY");
		SetCaption("THE SYNDICATE IS DEAD. ITS FRACTIONS ARE NOT.");

		await ToSignal(GetTree(), "process_frame");
		DriftStill(StakesSeconds);
		await WaitSeconds(StakesSeconds);
	}

	/// <summary>The closing wordmark over the action art.</summary>
	private async Task RunTitleBeat()
	{
		LayoutTitleText();

		await DissolveToWorld(BuildStills(TitleArt));

		// The story caption has done its job; the wordmark speaks for itself now.
		ClearCaption();
		ClearTag();

		_titleText.Visible = true;
		_subtitleText.Visible = true;
		_titleScrim.Visible = true;

		await ToSignal(GetTree(), "process_frame");
		DriftStill(TitleSeconds);
		await WaitSeconds(TitleSeconds);
	}

	// ---- the battle ---------------------------------------------------------

	/// <summary>
	/// Puts the fight in its own viewport and starts it. It runs for the whole
	/// film: the world beats are played over the top of a battle that keeps
	/// going, which is what makes the fight at the end the same fight the
	/// viewer saw in the first three seconds.
	/// </summary>
	private void StartBattle()
	{
		var encounter = AiVsAiRunner.CreateAiVsAiEncounter(seed: CombatSeed);
		BattleController.NextEncounterOverride = encounter;
		BattleController.StaticCameraOverride = true;

		var battleInstance = GD.Load<PackedScene>("res://scenes/battle.tscn").Instantiate();
		_battleViewport.GetNode<Node>("CurrentScene").AddChild(battleInstance);
		_battle = battleInstance.GetNode<BattleController>(".");

		_battleTask = RunBattleAsync();
	}

	private async Task RunBattleAsync()
	{
		// One frame for BattleController._Ready() to create the unit views.
		await ToSignal(GetTree(), "process_frame");

		var fighterId = FindUnitId("fighter");
		if (fighterId is not null)
		{
			_battle!.FocusUnitCamera(
				fighterId,
				HookCameraDistance,
				HookCameraYaw,
				HookCameraPitch,
				HookCameraRoll);
		}
		else
		{
			_battle!.SetupOverviewCamera();
		}

		foreach (var id in _battle!.BattleView.UnitViews.Keys)
		{
			_knownUnits.Add(id);
		}

		_battleCancel = new CancellationTokenSource();
		_battleSimulation = _battle.RunAiVsAiAsync(
			maxTurns: 8,
			onTurnLog: (turn, msg) => GD.Print($"[Teaser Battle] Turn {turn}: {msg}"),
			cancellationToken: _battleCancel.Token);
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
			startDistance: FightCameraDistance,
			dollySeconds: RevealDollySeconds,
			roll: RevealRoll,
			orbitAmplitude: RevealOrbitAmplitude,
			distance: RevealDistance);
	}

	/// <summary>
	/// Puts the fight on its own framing and arms the reveal. The pull-back only
	/// happens for a loss that the audience sees, so the move always has a cause
	/// in the picture.
	/// </summary>
	private void ArmFightFraming()
	{
		if (_battle is null)
			return;

		var subjectId = FindSubjectId();
		if (subjectId is not null)
		{
			_battle.FocusUnitCamera(
				subjectId,
				FightCameraDistance,
				FightCameraYaw,
				FightCameraPitch,
				FightCameraRoll);
		}

		_revealArmed = true;
		GD.Print($"[Teaser] fight framing armed at {_elapsed:F2}s, subject={subjectId}");
	}

	/// <summary>
	/// Picks the ship the fight shot is about. The player's fighter is the
	/// subject when it is still flying; when it is not, the shot falls back to
	/// another hull rather than to an empty part of the field.
	/// </summary>
	private string? FindSubjectId()
	{
		return FindUnitId("fighter")
			?? FindUnitId("gunship")
			?? FindUnitId("carrier")
			?? FindAnyVisibleUnit();
	}

	private string? FindAnyVisibleUnit()
	{
		if (_battle is null)
			return null;

		foreach (var pair in _battle.BattleView.UnitViews)
		{
			if (pair.Value.Visible)
				return pair.Key;
		}

		return null;
	}

	private void StopBattle()
	{
		_battleCancel?.Cancel();
		_battle?.StopReplay();
	}

	private string? FindUnitId(string marker)
	{
		if (_battle is null)
			return null;

		foreach (var id in _battle.BattleView.UnitViews.Keys)
		{
			if (id.Contains(marker, StringComparison.OrdinalIgnoreCase))
				return id;
		}

		return null;
	}

	private int EnemyCount()
	{
		if (_battle is null)
			return 0;

		return _battle.Battle.Layout.Participants.Values.Count(team => team == ETeam.Enemy);
	}

	// ---- the cost report ----------------------------------------------------

	private sealed record FleetCard(
		ETeam Team,
		EType Type,
		string Name,
		int Hull,
		bool Alive,
		string? PortraitPath);

	/// <summary>
	/// Reads the battle's own fleets and hull numbers. The report on screen is the
	/// simulation's, not a mock-up. Spawned ordnance (the void bombs in flight and
	/// the deployed miners) is not a hull and is left out.
	/// </summary>
	private List<FleetCard> BuildDamageReport()
	{
		var cards = new List<FleetCard>();
		if (_battle is null)
			return cards;

		var units = UnitRegistry.For(_battle.Battle.Engine.World).All;
		foreach (var unit in units)
		{
			var state = unit.State;
			if (state.ParentId != BattleActorIds.Rules)
				continue;

			cards.Add(new FleetCard(
				unit.Team,
				state.Type,
				ShortName(state.Id),
				state.HullPoints,
				state.IsAlive,
				PortraitFor(state.Type)));
		}

		return cards;
	}

	/// <summary>
	/// The card names come from the unit ids the simulation uses, e.g.
	/// "void_bomb-profound-boa-naron" becomes "PROFOUND-BOA-NARON".
	/// </summary>
	private static string ShortName(string unitId)
	{
		var trimmed = unitId;
		var dash = trimmed.IndexOf('-');
		if (dash >= 0)
			trimmed = trimmed[(dash + 1)..];

		return trimmed.ToUpperInvariant();
	}

	private static string? PortraitFor(EType type) => type switch
	{
		EType.Fighter => "res://assets/ui/unit_portraits/fighter.png",
		EType.Carrier => "res://assets/ui/unit_portraits/carrier.png",
		EType.VoidBomb => "res://assets/ui/unit_portraits/void_bomb.png",
		EType.RepurposedMiner => "res://assets/ui/unit_portraits/repurposed-miner.png",
		_ => null,
	};

	private static string TypeLabel(EType type) => type switch
	{
		EType.Fighter => "FIGHTER",
		EType.Gunship => "GUNSHIP",
		EType.IndustrialGooper => "GOOPER",
		EType.Carrier => "CARRIER",
		EType.RepurposedMiner => "REPURPOSED MINER",
		EType.VoidBomb => "VOID BOMB",
		_ => type.ToString().ToUpperInvariant(),
	};

	/// <summary>
	/// The caption for the cost beat. It says what the simulation actually did to
	/// the fleet, so the film cannot claim a loss that did not happen.
	/// </summary>
	private static string CostCaption(IReadOnlyList<FleetCard> cards)
	{
		var lost = cards.Where(card => card.Team == ETeam.Player && !card.Alive).ToList();
		if (lost.Count == 0)
			return "THE ROUTE IS CLEAR. THE WORK IS NOT.";

		if (lost.Count == 1)
			return $"THE {TypeLabel(lost[0].Type)} IS SCRAP. THE CONTRACT ISN'T PAID.";

		return "BOTH HULLS ARE SCRAP. THE CONTRACT ISN'T PAID.";
	}

	/// <summary>
	/// Builds the damage report as a staged 2D scene: the player's hulls on the
	/// left, the other side on the right, with the wreck marked.
	/// </summary>
	private Control BuildReportScene(IReadOnlyList<FleetCard> cards)
	{
		var size = (Vector2)_viewport.Size;
		var root = new Control
		{
			Position = Vector2.Zero,
			Size = size,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};

		root.AddChild(new ColorRect
		{
			Color = new Color(0.035f, 0.05f, 0.075f, 0.97f),
			Position = Vector2.Zero,
			Size = size,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		});

		const float cardWidth = 300f;
		const float cardGap = 60f;
		const float cardTop = 330f;

		var yours = cards.Where(card => card.Team == ETeam.Player).ToList();
		var theirs = cards.Where(card => card.Team != ETeam.Player).ToList();

		// The two fleets face each other across the middle of the frame, with the
		// divider where the fight was.
		var leftWidth = yours.Count * cardWidth + (yours.Count - 1) * cardGap;
		var rightWidth = theirs.Count * cardWidth + (theirs.Count - 1) * cardGap;
		var dividerWidth = 220f;
		var total = leftWidth + dividerWidth + rightWidth;
		var startX = (size.X - total) * 0.5f;

		var x = startX;
		foreach (var card in yours)
		{
			root.AddChild(BuildCard(card, new Vector2(x, cardTop)));
			x += cardWidth + cardGap;
		}

		root.AddChild(new Label
		{
			Text = "VS",
			Position = new Vector2(x, cardTop + 120f),
			Size = new Vector2(dividerWidth, 80f),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			LabelSettings = new LabelSettings
			{
				FontSize = 48,
				FontColor = new Color(0.45f, 0.52f, 0.6f),
			},
			MouseFilter = Control.MouseFilterEnum.Ignore,
		});

		x += dividerWidth;
		foreach (var card in theirs)
		{
			root.AddChild(BuildCard(card, new Vector2(x, cardTop)));
			x += cardWidth + cardGap;
		}

		return root;
	}

	private static Control BuildCard(FleetCard card, Vector2 at)
	{
		const float size = 300f;
		var cardRoot = new Control
		{
			Position = at,
			Size = new Vector2(size, size + 100f),
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};

		if (card.PortraitPath is not null)
		{
			cardRoot.AddChild(new TextureRect
			{
				Texture = GD.Load<Texture2D>(card.PortraitPath),
				StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
				Position = Vector2.Zero,
				Size = new Vector2(size, size),
				Modulate = card.Alive ? Colors.White : new Color(0.62f, 0.42f, 0.42f),
				MouseFilter = Control.MouseFilterEnum.Ignore,
			});
		}
		else
		{
			// No painted portrait for this hull, so it is shown as a data chip
			// rather than an empty frame.
			var chip = new PanelContainer
			{
				Position = Vector2.Zero,
				Size = new Vector2(size, size),
				MouseFilter = Control.MouseFilterEnum.Ignore,
			};
			chip.AddThemeStyleboxOverride("panel", new StyleBoxFlat
			{
				BgColor = card.Alive
					? new Color(0.08f, 0.12f, 0.18f, 1f)
					: new Color(0.16f, 0.07f, 0.08f, 1f),
				BorderColor = card.Alive
					? new Color(0.25f, 0.38f, 0.52f, 1f)
					: new Color(0.48f, 0.18f, 0.17f, 1f),
				BorderWidthLeft = 2,
				BorderWidthTop = 2,
				BorderWidthRight = 2,
				BorderWidthBottom = 2,
			});
			chip.AddChild(new Label
			{
				Text = TypeLabel(card.Type),
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				LabelSettings = new LabelSettings
				{
					FontSize = 34,
					FontColor = card.Alive
						? new Color(0.45f, 0.58f, 0.7f)
						: new Color(0.62f, 0.32f, 0.3f),
				},
				MouseFilter = Control.MouseFilterEnum.Ignore,
			});
			cardRoot.AddChild(chip);
		}

		cardRoot.AddChild(new Label
		{
			Text = TypeLabel(card.Type),
			Position = new Vector2(0f, size + 12f),
			Size = new Vector2(size, 44f),
			HorizontalAlignment = HorizontalAlignment.Center,
			LabelSettings = new LabelSettings
			{
				FontSize = 32,
				FontColor = card.Alive ? new Color(0.86f, 0.91f, 0.95f) : new Color(0.78f, 0.36f, 0.34f),
			},
			MouseFilter = Control.MouseFilterEnum.Ignore,
		});

		cardRoot.AddChild(new Label
		{
			Text = card.Alive ? $"HULL {card.Hull}" : "SCRAP",
			Position = new Vector2(0f, size + 58f),
			Size = new Vector2(size, 34f),
			HorizontalAlignment = HorizontalAlignment.Center,
			LabelSettings = new LabelSettings
			{
				FontSize = 26,
				FontColor = card.Alive ? new Color(0.55f, 0.62f, 0.7f) : new Color(0.88f, 0.34f, 0.3f),
			},
			MouseFilter = Control.MouseFilterEnum.Ignore,
		});

		return cardRoot;
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

			// The world layer has to be visible for the fade to be seen; the
			// incoming content then dissolves up out of the battle underneath.
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
	/// Slow push and a small tilt on a held still, so the paintings move instead
	/// of standing there.
	/// </summary>
	private async Task DriftStill(float seconds)
	{
		if (_current is not TextureRect still)
		{
			await WaitSeconds(seconds);
			return;
		}

		var tween = CreateTween();
		tween.SetParallel(true);
		tween.TweenProperty(still, "scale", new Vector2(StillPush, StillPush), seconds)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
		tween.TweenProperty(
				still,
				"rotation",
				Mathf.DegToRad(StillTiltDegrees),
				seconds)
			.SetTrans(Tween.TransitionType.Sine);
		await WaitSeconds(seconds);
	}

	/// <summary>
	/// A slow push on a staged 2D scene (the station, the damage report), so
	/// every held shot in the film has movement in it.
	/// </summary>
	private void PushInOnScene(Node scene, float seconds)
	{
		if (scene is not Control control)
			return;

		control.PivotOffset = control.Size * 0.5f;
		var tween = CreateTween();
		tween.TweenProperty(control, "scale", new Vector2(1.05f, 1.05f), seconds)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
	}

	/// <summary>
	/// Pushes the strategic map in and tips the horizon, so the establishing shot
	/// is a place and not a diagram.
	/// </summary>
	private void PushInOnMap(Node mapInstance, float seconds)
	{
		if (mapInstance.GetNodeOrNull<MapCamera>("CameraPivot/SpringArm3D/Camera3D") is not { } camera)
			return;

		camera.Roll = MapRoll;
		var pose = camera.CurrentPose;
		camera.TweenToPose(
			new OrbitPose
			{
				Pivot = pose.Pivot,
				Yaw = pose.Yaw + 0.20f,
				Pitch = Mathf.Max(pose.Pitch - 0.07f, 0.12f),
				Distance = pose.Distance * 0.76f,
			},
			seconds);
	}

	// ---- helpers ------------------------------------------------------------

	/// <summary>Builds a full-screen still from a story page.</summary>
	private TextureRect BuildStills(string texturePath)
	{
		var viewportSize = (Vector2)_viewport.Size;
		return new TextureRect
		{
			Texture = GD.Load<Texture2D>(texturePath),
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
			Position = Vector2.Zero,
			Size = viewportSize,
			PivotOffset = viewportSize * 0.5f,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
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

	/// <summary>Points the facility scenes at a real POI so travel shows its live binding.</summary>
	private void EnterTravelFacility()
	{
		var map = Session.Instance.Run?.StarSystem?.Map;
		if (map is null)
			return;

		// The travel scene belongs to the wormhole's travel facility; any other
		// facility has a different scene layout and its binding would fail.
		foreach (var poi in map.PointsOfInterest)
		{
			var facility = poi.Facilities.FirstOrDefault(candidate =>
				candidate.ScenePath == TravelScenePath);
			if (facility is null)
				continue;

			MapNavigationContext.EnterFacility(poi.Id, facility.Id);
			return;
		}
	}

	// ---- overlay ------------------------------------------------------------

	/// <summary>
	/// The story caption: one line of text, bottom left, that says what is
	/// happening to the pilot rather than what the game has.
	/// </summary>
	private void BuildCaption()
	{
		var overlay = GetNode<Node>("Overlay");

		var scrim = new TextureRect
		{
			Texture = new GradientTexture2D
			{
				Gradient = new Gradient
				{
					Offsets = new[] { 0f, 1f },
					Colors = new[] { new Color(0f, 0f, 0f, 0.78f), new Color(0f, 0f, 0f, 0f) },
				},
				FillFrom = new Vector2(0f, 0f),
				FillTo = new Vector2(1f, 0f),
				Width = 512,
				Height = 8,
			},
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.Scale,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			ZIndex = 98,
		};
		scrim.SetAnchorsPreset(Control.LayoutPreset.BottomLeft);
		scrim.OffsetLeft = 0f;
		scrim.OffsetRight = 860f;
		scrim.OffsetTop = -190f;
		scrim.OffsetBottom = 0f;
		scrim.Visible = false;
		overlay.AddChild(scrim);

		_captionText = new Label
		{
			LabelSettings = new LabelSettings
			{
				FontSize = 32,
				FontColor = new Color(0.93f, 0.96f, 1f),
				OutlineSize = 9,
				OutlineColor = new Color(0f, 0f, 0f, 0.92f),
				ShadowSize = 5,
				ShadowColor = new Color(0f, 0f, 0f, 0.7f),
			},
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Bottom,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Visible = false,
			ZIndex = 99,
		};
		// The caption lives in the bottom-left corner, clear of the battle HUD's
		// action bar and of the map's narrative box.
		_captionText.SetAnchorsPreset(Control.LayoutPreset.BottomWide);
		_captionText.OffsetLeft = 105f;
		_captionText.OffsetRight = -1180f;
		_captionText.OffsetTop = -150f;
		_captionText.OffsetBottom = -18f;
		overlay.AddChild(_captionText);

		// The corner tag names the system, so the genre still reads without the
		// caption turning into a feature list. It sits clear of the battle HUD's
		// hull bars in the top-left corner.
		_phaseLabel.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
		_phaseLabel.OffsetLeft = 200f;
		_phaseLabel.OffsetTop = 52f;
		_phaseLabel.OffsetRight = 900f;
		_phaseLabel.OffsetBottom = 104f;
		_phaseLabel.HorizontalAlignment = HorizontalAlignment.Left;
		_phaseLabel.LabelSettings = new LabelSettings
		{
			FontSize = 24,
			FontColor = new Color(0.60f, 0.67f, 0.75f),
			OutlineSize = 6,
			OutlineColor = new Color(0f, 0f, 0f, 0.8f),
		};
	}

	private void BuildTitleScrim()
	{
		// Soft gradient under the closing wordmark so it reads over the action art
		// without stamping a hard-edged band across the frame.
		var scrimGradient = new Gradient();
		scrimGradient.SetColor(0, new Color(0f, 0f, 0f, 0f));
		scrimGradient.SetColor(1, new Color(0f, 0f, 0f, 0.9f));
		_titleScrim = new TextureRect
		{
			Texture = new GradientTexture2D
			{
				Gradient = scrimGradient,
				FillFrom = new Vector2(0f, 0f),
				FillTo = new Vector2(0f, 1f),
				Width = 8,
				Height = 256,
			},
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.Scale,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			ZIndex = 99,
		};
		_titleScrim.SetAnchorsPreset(Control.LayoutPreset.BottomWide);
		_titleScrim.OffsetTop = -520f;
		_titleScrim.OffsetBottom = 0f;
		_titleScrim.Visible = false;
		GetNode<Node>("Overlay").AddChild(_titleScrim);
	}

	/// <summary>Sets the story caption for the current beat.</summary>
	private void SetCaption(string text)
	{
		_captionText.Text = text;
		_captionText.Visible = true;
	}

	/// <summary>Removes the story caption.</summary>
	private void ClearCaption()
	{
		_captionText.Visible = false;
		_captionText.Text = string.Empty;
	}

	/// <summary>Names the system on screen, in the corner.</summary>
	private void SetTag(string text)
	{
		_phaseLabel.Text = text;
		_phaseLabel.Visible = true;
	}

	/// <summary>Removes the corner tag.</summary>
	private void ClearTag()
	{
		_phaseLabel.Visible = false;
		_phaseLabel.Text = string.Empty;
	}

	/// <summary>Places the wordmark and tagline on the lower third, clear of the captions.</summary>
	private void LayoutTitleText()
	{
		PlaceLabel(_titleText, 690f, 860f);
		PlaceLabel(_subtitleText, 880f, 950f);

		_titleText.LabelSettings = new LabelSettings
		{
			FontSize = 86,
			FontColor = new Color(0.97f, 0.98f, 1f),
			OutlineSize = 12,
			OutlineColor = new Color(0f, 0f, 0f, 0.9f),
			ShadowSize = 8,
			ShadowColor = new Color(0f, 0f, 0f, 0.75f),
		};
		_subtitleText.LabelSettings = new LabelSettings
		{
			FontSize = 34,
			FontColor = new Color(0.74f, 0.80f, 0.88f),
			OutlineSize = 8,
			OutlineColor = new Color(0f, 0f, 0f, 0.85f),
		};
	}

	/// <summary>
	/// Places a full-width overlay label at an absolute vertical band. The label's
	/// anchors span the viewport, so its offsets have to be expressed relative to
	/// those anchors.
	/// </summary>
	private void PlaceLabel(Label label, float top, float bottom)
	{
		var height = _viewport.Size.Y;
		label.OffsetLeft = 0f;
		label.OffsetRight = 0f;
		label.OffsetTop = top;
		label.OffsetBottom = bottom - height;
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

	public override void _Process(double delta)
	{
		_elapsed += delta;
		UpdateBattleCamera();
	}
}

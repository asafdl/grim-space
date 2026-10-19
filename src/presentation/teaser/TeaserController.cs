using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using GrimSpace.Application;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Presentation.Scene;
using GrimSpace.World.StarSystem.Presentation.Scene;

namespace GrimSpace.Presentation.Teaser;

/// <summary>
/// Directs the teaser video.
///
/// The cut list is fixed and every shot is live game content: a tactical engagement,
/// the story art, the strategic command layer, a facility interior, the roster and the
/// closing wordmark. Nothing in the video is mocked up.
///
/// Transitions are real cross-dissolves. Earlier revisions faded the frame out to
/// black and back to the *same* image before hard-cutting to the next shot, which
/// strobed the picture to black a dozen times; the dissolves here overlap the outgoing
/// and incoming images instead, so the frame never passes through black.
/// </summary>
public partial class TeaserController : Node
{
	// ---- shot list ---------------------------------------------------------
	// The hook is live combat: the viewer sees ships trading fire inside five seconds.
	private const float CombatHookSeconds = 5.0f;
	// The wide shot ends as soon as the engagement stops being legible: the last
	// ships die around 9.5 s and the frame would otherwise run on empty for four
	// seconds. Dead air is worse than a short cut.
	private const float CombatWideSeconds = 4.2f;
	private const float StoryPageSeconds = 2.2f;
	private const float DissolveSeconds = 0.6f;
	private const float MapSeconds = 5.4f;
	private const float TravelSeconds = 3.4f;
	private const float RosterSeconds = 1.5f;
	private const float TitleCardSeconds = 6.0f;
	private const float EndFadeSeconds = 1.0f;
	private const float OpeningFadeSeconds = 0.4f;

	// Hook framing: close enough to read a hull, loose enough to keep the enemy in
	// the background of the frame.
	private const float HookCameraDistance = 8.0f;
	private const float HookCameraYaw = 2.3f;
	private const float HookCameraPitch = 0.2f;

	// The pull back to the whole engagement starts where the close shot left off.
	private const float WideCameraStartDistance = 9f;
	private const float WideCameraDollySeconds = 2.4f;

	private const int CombatSeed = 99999;
	private const string TravelScenePath = "res://scenes/travel.tscn";

	private SubViewport _viewport = null!;
	private TextureRect _viewportDisplay = null!;
	ColorRect _transition = null!;
	Label _titleText = null!;
	Label _subtitleText = null!;
	Label _phaseLabel = null!;
	TextureRect _unitPortrait = null!;
	TextureRect _titleScrim = null!;

	/// <summary>
	/// The content currently on screen. Only one piece is live at a time: a still, a
	/// 2D scene or a 3D scene. The dissolve helpers cross-fade from it.
	/// </summary>
	private Node? _current;

	/// <summary>Story beats: the world humanity lost, told in two paintings.</summary>
	private readonly string[] _storyPages = new[]
	{
		"res://assets/story/pages/humanity-controls-galaxy.png",
		"res://assets/story/pages/total-war.png"
	};

	private readonly (string texture, string label)[] _unitRoster = new[]
	{
		("res://assets/ui/unit_portraits/fighter.png", "FIGHTER\nStrike Craft"),
		("res://assets/ui/unit_portraits/carrier.png", "CARRIER\nDrone Controller"),
		("res://assets/ui/unit_portraits/void_bomb.png", "VOID BOMB\nSuicide Bomber"),
		("res://assets/ui/unit_portraits/repurposed-miner.png", "REPURPOSED MINER\nUtility Drone")
	};

	public override void _Ready()
	{
		_viewport = GetNode<SubViewport>("ViewPort");
		_viewport.OwnWorld3D = true;
		_viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;

		// Display SubViewport texture so story artwork / battle units are visible
		_viewportDisplay = new TextureRect
		{
			Texture = _viewport.GetTexture(),
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			AnchorsPreset = (int)Control.LayoutPreset.FullRect
		};
		AddChild(_viewportDisplay);
		MoveChild(_viewportDisplay, 0); // Behind overlay

		_transition = GetNode<ColorRect>("Overlay/Transition");
		_titleText = GetNode<Label>("Overlay/TitleText");
		_subtitleText = GetNode<Label>("Overlay/SubtitleText");
		_phaseLabel = GetNode<Label>("Overlay/PhaseLabel");
		_unitPortrait = GetNode<TextureRect>("Overlay/UnitPortrait");

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

		if (Session.Instance.Run is null)
		{
			Session.Instance.StartNewRun();
		}

		// The frame-rate readout is a development overlay; it does not belong in a
		// recorded video.
		Session.ShowPerformanceOverlay = false;

		RunTeaser();
	}

	private async void RunTeaser()
	{
		try
		{
			await FadeIn(OpeningFadeSeconds);
			await RunCombatBeat();
			await RunTravelBeat();
			await RunMapBeat();
			await RunRosterBeat();
			await RunStoryBeat();
			await RunTitleBeat();
		}
		catch (Exception error)
		{
			GD.PushError($"[Teaser] Shot list failed: {error}");
		}

		// One single fade to black at the very end. Earlier revisions faded out and
		// then faded back in here, which strobed the last two seconds.
		await FadeOut(EndFadeSeconds);
		GetTree().Quit();
	}

	// ---- shots -------------------------------------------------------------

	/// <summary>
	/// The hook and the wide: one continuous engagement, opened on a hull close-up and
	/// pulled back to the whole fight. Real simulation throughout - the tracer fire,
	/// the damage numbers and the detonation are the game's own.
	/// </summary>
	private async Task RunCombatBeat()
	{
		_phaseLabel.Text = "TACTICAL COMBAT";
		_phaseLabel.Visible = true;

		var encounter = AiVsAiRunner.CreateAiVsAiEncounter(seed: CombatSeed);
		BattleController.NextEncounterOverride = encounter;
		BattleController.StaticCameraOverride = true;

		var battleInstance = GD.Load<PackedScene>("res://scenes/battle.tscn").Instantiate();
		StageScene(battleInstance);

		var battleController = battleInstance.GetNode<BattleController>(".");

		// Wait one frame for BattleController._Ready() to create unit views
		await ToSignal(GetTree(), "process_frame");

		var fighterId = FindUnitId(battleController, "fighter");
		if (fighterId is not null)
		{
			battleController.FocusUnitCamera(
				fighterId, HookCameraDistance, HookCameraYaw, HookCameraPitch);
		}
		else
		{
			battleController.SetupOverviewCamera();
		}

		using var cancel = new CancellationTokenSource();
		var battle = battleController.RunAiVsAiAsync(
			maxTurns: 8,
			onTurnLog: (turn, msg) => GD.Print($"[Teaser Battle] Turn {turn}: {msg}"),
			cancellationToken: cancel.Token);

		await WaitSeconds(CombatHookSeconds);

		// Pull back to the whole engagement. Starting from the close shot's distance
		// keeps it a continuous move instead of a jump.
		battleController.SetupOverviewCamera(
			startDistance: WideCameraStartDistance,
			dollySeconds: WideCameraDollySeconds);

		await WaitSeconds(CombatWideSeconds);

		battleController.StopReplay();
		cancel.Cancel();
		try
		{
			await battle;
		}
		catch (OperationCanceledException)
		{
			// Expected: the teaser cut the beat on purpose.
		}

		_phaseLabel.Visible = false;
	}

	/// <summary>Two story paintings, each with a slow push so the frame is never dead.</summary>
	private async Task RunStoryBeat()
	{
		foreach (var page in _storyPages)
		{
			await DissolveToStill(page, DissolveSeconds);
			_phaseLabel.Text = "THE FALL OF HUMANITY";
			_phaseLabel.Visible = true;
			await DriftStill(StoryPageSeconds);
		}

		_phaseLabel.Visible = false;
	}

	/// <summary>The strategic layer: the map the run is played on.</summary>
	private async Task RunMapBeat()
	{
		// The travel beat entered a facility, which makes the map open on that POI's facade.
		// The teaser wants the wide strategic view instead.
		MapNavigationContext.ClearReturnToFacade();

		var mapInstance = GD.Load<PackedScene>("res://scenes/map.tscn").Instantiate();
		await DissolveToScene(mapInstance, DissolveSeconds);

		_phaseLabel.Text = "STRATEGIC MAP";
		_phaseLabel.Visible = true;

		await WaitSeconds(MapSeconds);
		_phaseLabel.Visible = false;
	}

	/// <summary>A facility interior: the places between battles.</summary>
	private async Task RunTravelBeat()
	{
		// Facility scenes bind to a live POI in the run; without one they render but log an error.
		EnterTravelFacility();

		var travelInstance = GD.Load<PackedScene>(TravelScenePath).Instantiate();
		await DissolveToScene(travelInstance, DissolveSeconds);

		_phaseLabel.Text = "SPACE TRAVEL & FACILITIES";
		_phaseLabel.Visible = true;

		await WaitSeconds(TravelSeconds);
		_phaseLabel.Visible = false;
	}

	/// <summary>The four ships the player commands, one card each.</summary>
	private async Task RunRosterBeat()
	{
		var backdrop = new ColorRect
		{
			Color = new Color(0.03f, 0.04f, 0.07f),
			Position = Vector2.Zero,
			Size = _viewport.Size,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		await DissolveToScene(backdrop, DissolveSeconds);

		_phaseLabel.Text = "UNIT ROSTER";
		_phaseLabel.Visible = true;
		_unitPortrait.Visible = true;
		SetPortraitSize(620f);

		foreach (var (texturePath, label) in _unitRoster)
		{
			_unitPortrait.Texture = GD.Load<Texture2D>(texturePath);
			_phaseLabel.Text = label;

			await FadePortraitIn(0.25f);
			await WaitSeconds(RosterSeconds);
			await FadePortraitOut(0.2f);
		}

		SetPortraitSize(400f);
		_unitPortrait.Visible = false;
		_phaseLabel.Visible = false;
	}

	/// <summary>The closing wordmark over the action art.</summary>
	private async Task RunTitleBeat()
	{
		// The wordmark sits on the title art, clear of the phase caption at the bottom.
		LayoutTitleText();

		await DissolveToStill("res://assets/story/pages/war-and-ruin2.png", DissolveSeconds);

		_titleText.Visible = true;
		_subtitleText.Visible = true;
		_titleScrim.Visible = true;

		await WaitSeconds(TitleCardSeconds);
	}

	// ---- transitions -------------------------------------------------------

	/// <summary>
	/// Cross-fades to a full-screen still. The incoming image fades in on top of the
	/// outgoing one; the frame never goes through black.
	/// </summary>
	private async Task DissolveToStill(string texturePath, float seconds)
	{
		var viewportSize = (Vector2)_viewport.Size;
		var still = new TextureRect
		{
			Texture = GD.Load<Texture2D>(texturePath),
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
			Position = Vector2.Zero,
			Size = viewportSize,
			PivotOffset = viewportSize * 0.5f,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};

		await DissolveTo(still, seconds);
	}

	/// <summary>
	/// Dissolves to a staged scene. A 2D scene fades up over whatever is behind it; a
	/// 3D scene is revealed by fading the still away from in front of it.
	/// </summary>
	private async Task DissolveToScene(Node scene, float seconds)
	{
		await DissolveTo(scene, seconds);
	}

	/// <summary>
	/// The one transition primitive. 2D content is drawn in the viewport's canvas, in
	/// front of 3D content, so 2D content fades up over what is behind it and 3D
	/// content is uncovered by fading the 2D layer away from in front of it.
	/// </summary>
	private async Task DissolveTo(Node incoming, float seconds)
	{
		var root = _viewport.GetNode<Node>("CurrentScene");
		var outgoing = _current;

		if (incoming is CanvasItem canvasItem)
		{
			// The incoming 2D content is drawn on top of the outgoing 2D content and
			// in front of the 3D world, so fading it up is the dissolve.
			canvasItem.Modulate = new Color(1, 1, 1, 0);
			root.AddChild(incoming);
			_current = incoming;

			var tween = CreateTween();
			tween.TweenProperty(canvasItem, "modulate:a", 1f, seconds)
				.SetTrans(Tween.TransitionType.Sine);
			await ToSignal(tween, "finished");
		}
		else
		{
			// The 3D world renders behind the 2D canvas, so the outgoing still is what
			// has to fade for the new scene to show through.
			if (outgoing is CanvasItem)
			{
				await ToSignal(GetTree(), "process_frame");
				await ToSignal(GetTree(), "process_frame");
			}

			root.AddChild(incoming);
			_current = incoming;

			if (outgoing is CanvasItem outgoingItem)
			{
				var tween = CreateTween();
				tween.TweenProperty(outgoingItem, "modulate:a", 0f, seconds)
					.SetTrans(Tween.TransitionType.Sine);
				await ToSignal(tween, "finished");
			}
		}

		FreeStaged(outgoing);
	}

	/// <summary>
	/// Slow push on the current still so a held painting still moves. Prevents the
	/// dead-air slideshow the earlier revision had.
	/// </summary>
	private async Task DriftStill(float seconds)
	{
		if (_current is not TextureRect still)
		{
			await WaitSeconds(seconds);
			return;
		}

		var tween = CreateTween();
		tween.TweenProperty(still, "scale", new Vector2(1.08f, 1.08f), seconds)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);

		await WaitSeconds(seconds);
	}

	// ---- helpers -----------------------------------------------------------

	/// <summary>Adds a scene to the viewport, replacing whatever was staged before.</summary>
	private void StageScene(Node scene)
	{
		ClearStagedContent();
		var root = _viewport.GetNode<Node>("CurrentScene");
		root.AddChild(scene);
		_current = scene;
	}

	/// <summary>
	/// Removes a node from the viewport. Nodes are detached before they are freed so
	/// their canvases stop drawing on the very same frame.
	/// </summary>
	private void FreeStaged(Node? node)
	{
		if (node is null || !GodotObject.IsInstanceValid(node))
			return;

		var root = _viewport.GetNode<Node>("CurrentScene");
		if (node.GetParent() == root)
			root.RemoveChild(node);
		node.QueueFree();
	}

	/// <summary>Empties the viewport of everything that was staged.</summary>
	private void ClearStagedContent()
	{
		var root = _viewport.GetNode<Node>("CurrentScene");
		foreach (var child in root.GetChildren())
		{
			root.RemoveChild(child);
			child.QueueFree();
		}

		_current = null;
	}

	/// <summary>Finds the id of a unit whose id contains the given marker.</summary>
	private static string? FindUnitId(BattleController battle, string marker)
	{
		foreach (var id in battle.BattleView.UnitViews.Keys)
		{
			if (id.Contains(marker, StringComparison.OrdinalIgnoreCase))
				return id;
		}

		return null;
	}

	/// <summary>Points the facility scenes at a real POI so travel shows its live binding.</summary>
	private void EnterTravelFacility()
	{
		var map = Session.Instance.Run?.StarSystem?.Map;
		if (map is null)
			return;

		// The travel scene belongs to the wormhole's travel facility; any other facility
		// has a different scene layout and its binding would fail.
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

	/// <summary>Resizes the centred unit portrait without moving it off screen centre.</summary>
	private void SetPortraitSize(float size)
	{
		var half = size * 0.5f;
		_unitPortrait.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
		_unitPortrait.OffsetLeft = -half;
		_unitPortrait.OffsetTop = -half;
		_unitPortrait.OffsetRight = half;
		_unitPortrait.OffsetBottom = half;
	}

	/// <summary>Places the wordmark and tagline on the lower third, clear of the captions.</summary>
	private void LayoutTitleText()
	{
		PlaceLabel(_titleText, 700f, 870f);
		PlaceLabel(_subtitleText, 880f, 945f);
	}

	/// <summary>
	/// Places a full-width overlay label at an absolute vertical band. The label's anchors
	/// span the viewport, so its offsets have to be expressed relative to those anchors.
	/// </summary>
	private void PlaceLabel(Label label, float top, float bottom)
	{
		var height = _viewport.Size.Y;
		label.OffsetLeft = 0f;
		label.OffsetRight = 0f;
		label.OffsetTop = top;
		label.OffsetBottom = bottom - height;
	}

	private async Task FadePortraitIn(float seconds)
	{
		_unitPortrait.Modulate = new Color(1, 1, 1, 0);
		var tween = CreateTween();
		tween.TweenProperty(_unitPortrait, "modulate:a", 1f, seconds)
			.SetTrans(Tween.TransitionType.Sine);
		await ToSignal(tween, "finished");
	}

	private async Task FadePortraitOut(float seconds)
	{
		var tween = CreateTween();
		tween.TweenProperty(_unitPortrait, "modulate:a", 0f, seconds)
			.SetTrans(Tween.TransitionType.Sine);
		await ToSignal(tween, "finished");
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
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using GrimSpace.Application;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Presentation.Scene;
using GrimSpace.Presentation.Intro;
using GrimSpace.Presentation.Menu;
using GrimSpace.World.StarSystem.Presentation.Scene;

namespace GrimSpace.Presentation.Teaser;

public partial class TeaserController : Node
{
	private enum TeaserPhase
	{
		Opening,
		StoryArtwork,
		TacticalCombat,
		SpaceTravel,
		UnitRoster,
		StrategicMap,
		TitleCard,
		Complete
	}

	// Teaser pacing. Every phase has a hard duration so the cut never has dead air.
	private const float OpeningSeconds = 4.0f;
	private const float StoryPageSeconds = 2.4f;
	private const float StoryCrossfadeSeconds = 0.45f;
	private const float CombatBeatSeconds = 26.0f;
	private const float TravelSeconds = 6.0f;
	private const float RosterSeconds = 2.2f;
	private const float MapSeconds = 7.0f;
	private const float TitleCardSeconds = 5.0f;
	private const float CombatHoldSeconds = 1.2f;
	private const string TravelScenePath = "res://scenes/travel.tscn";

	private SubViewport _viewport = null!;
	private TextureRect _viewportDisplay = null!;
	Node? _currentSceneInstance = null;
	ColorRect _transition = null!;
	Label _titleText = null!;
	Label _subtitleText = null!;
	Label _phaseLabel = null!;
	TextureRect _unitPortrait = null!;
	ColorRect _titleScrim = null!;

	private TeaserPhase _currentPhase = TeaserPhase.Opening;
	private int _storyPageIndex = 0;

	/// <summary>Curated story beats: hope, escalation, the new world, the threat, the fall.</summary>
	private readonly string[] _storyPages = new[]
	{
		"res://assets/story/pages/humanity-controls-galaxy.png",
		"res://assets/story/pages/total-war.png",
		"res://assets/story/pages/new-humanlike-civilization.png",
		"res://assets/story/pages/machine-emotion.png",
		"res://assets/story/pages/humanity-loses-control.png",
		"res://assets/story/pages/war-and-ruin2.png"
	};

	private readonly (string texture, string label)[] _unitRoster = new[]
	{
		("res://assets/ui/unit_portraits/fighter.png", "FIGHTER\nStrike Craft"),
		("res://assets/ui/unit_portraits/carrier.png", "CARRIER\nDrone Controller"),
		("res://assets/ui/unit_portraits/void_bomb.png", "VOID BOMB\nSuicide Bomber"),
		("res://assets/ui/unit_portraits/repurposed-miner.png", "REPURPOSED MINER\nUtility Drone")
	};

	private int _unitIndex = 0;

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

		// Scrim under the closing wordmark so it reads over the action art.
		_titleScrim = new ColorRect
		{
			Color = new Color(0f, 0f, 0f, 0.55f),
			MouseFilter = Control.MouseFilterEnum.Ignore,
			ZIndex = 99,
		};
		_titleScrim.SetAnchorsPreset(Control.LayoutPreset.BottomWide);
		_titleScrim.OffsetTop = -440f;
		_titleScrim.OffsetBottom = 0f;
		_titleScrim.Visible = false;
		GetNode<Node>("Overlay").AddChild(_titleScrim);

		if (Session.Instance.Run is null)
		{
			Session.Instance.StartNewRun();
		}

		StartPhase(TeaserPhase.Opening);
	}

	private async void StartPhase(TeaserPhase phase)
	{
		_currentPhase = phase;
		await FadeIn(0.5f);

		switch (phase)
		{
			case TeaserPhase.Opening:
				await RunOpeningPhase();
				break;
			case TeaserPhase.StoryArtwork:
				await RunStoryArtworkPhase();
				break;
			case TeaserPhase.TacticalCombat:
				await RunTacticalCombatPhase();
				break;
			case TeaserPhase.SpaceTravel:
				await RunSpaceTravelPhase();
				break;
			case TeaserPhase.UnitRoster:
				await RunUnitRosterPhase();
				break;
			case TeaserPhase.StrategicMap:
				await RunStrategicMapPhase();
				break;
			case TeaserPhase.TitleCard:
				await RunTitleCardPhase();
				break;
		}

		await FadeOut(0.5f);

		if (phase != TeaserPhase.Complete)
		{
			StartPhase((TeaserPhase)((int)phase + 1));
		}
		else
		{
			GetTree().Quit();
		}
	}

	/// <summary>Logo open: the game's title art with the tagline.</summary>
	private async Task RunOpeningPhase()
	{
		await ShowFullScreenArt("res://assets/story/pages/intro-screen.png", 0.55f);

		// The title art already carries the wordmark; only the tagline is overlaid.
		_subtitleText.Visible = true;
		LayoutSubtitleText();

		await ToSignal(GetTree().CreateTimer(OpeningSeconds), "timeout");

		_subtitleText.Visible = false;
	}

	/// <summary>Places the tagline in the lower third, clear of the captions.</summary>
	private void LayoutSubtitleText()
	{
		PlaceLabel(_subtitleText, 880f, 945f);
	}

	private async Task RunStoryArtworkPhase()
	{
		_phaseLabel.Text = "THE FALL OF HUMANITY";
		_phaseLabel.Visible = true;

		for (_storyPageIndex = 0; _storyPageIndex < _storyPages.Length; _storyPageIndex++)
		{
			await ShowStoryPage(_storyPages[_storyPageIndex]);
			await ToSignal(GetTree().CreateTimer(StoryPageSeconds), "timeout");
			await FadeTransition(StoryCrossfadeSeconds);
		}

		_phaseLabel.Visible = false;
	}

	private async Task ShowStoryPage(string texturePath)
	{
		await ShowFullScreenArt(texturePath, 1.0f);
	}

	/// <summary>Swaps the viewport content to a full-screen still and fades it in.</summary>
	private async Task ShowFullScreenArt(string texturePath, float fadeSeconds, float dim = 1.0f)
	{
		var texture = GD.Load<Texture2D>(texturePath);
		var textureRect = new TextureRect
		{
			Texture = texture,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
			Modulate = new Color(1, 1, 1, dim),
			Position = Vector2.Zero,
			Size = _viewport.Size,
		};

		ClearCurrentScene();
		_currentSceneInstance = textureRect;
		_viewport.GetNode("CurrentScene").AddChild(textureRect);

		await FadeIn(fadeSeconds);
	}

	/// <summary>Stages a flat colour behind the viewport content.</summary>
	private void ShowBackdropColor(Color color)
	{
		var backdrop = new ColorRect
		{
			Color = color,
			Position = Vector2.Zero,
			Size = _viewport.Size,
		};

		ClearCurrentScene();
		_currentSceneInstance = backdrop;
		_viewport.GetNode("CurrentScene").AddChild(backdrop);
	}

	private async Task RunTacticalCombatPhase()
	{
		_phaseLabel.Text = "TACTICAL COMBAT";
		_phaseLabel.Visible = true;

		var encounter = AiVsAiRunner.CreateAiVsAiEncounter(seed: 99999);
		BattleController.NextEncounterOverride = encounter;
		BattleController.StaticCameraOverride = true;

		var battleScene = GD.Load<PackedScene>("res://scenes/battle.tscn");
		var battleInstance = battleScene.Instantiate();

		ClearCurrentScene();
		_currentSceneInstance = battleInstance;
		_viewport.GetNode("CurrentScene").AddChild(battleInstance);

		var battleController = battleInstance.GetNode<BattleController>(".");

		// Wait one frame for BattleController._Ready() to create unit views
		await ToSignal(GetTree(), "process_frame");
		battleController.SetupOverviewCamera();

		await FadeIn(1.0f);

		using var cancel = new CancellationTokenSource();
		var battle = battleController.RunAiVsAiAsync(
			maxTurns: 8,
			onTurnLog: (turn, msg) => GD.Print($"[Teaser Battle] Turn {turn}: {msg}"),
			cancellationToken: cancel.Token);

		// Hold the combat beat for a fixed time, then cut. The battle keeps running to a
		// real outcome underneath; the teaser simply stops the shot before the result.
		var deadline = Time.GetTicksMsec() + (long)(CombatBeatSeconds * 1000f);
		while (!battle.IsCompleted && Time.GetTicksMsec() < deadline)
			await ToSignal(GetTree(), "process_frame");

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

		await ToSignal(GetTree().CreateTimer(CombatHoldSeconds), "timeout");
		_phaseLabel.Visible = false;
	}

	private async Task RunSpaceTravelPhase()
	{
		_phaseLabel.Text = "SPACE TRAVEL & FACILITIES";
		_phaseLabel.Visible = true;

		// Facility scenes bind to a live POI in the run; without one they render but log an error.
		EnterTravelFacility();

		var travelScene = GD.Load<PackedScene>(TravelScenePath);
		var travelInstance = travelScene.Instantiate();

		ClearCurrentScene();
		_currentSceneInstance = travelInstance;
		_viewport.GetNode("CurrentScene").AddChild(travelInstance);

		await FadeIn(1.0f);
		await ToSignal(GetTree().CreateTimer(TravelSeconds), "timeout");
		_phaseLabel.Visible = false;
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

	private async Task RunUnitRosterPhase()
	{
		// Clean card backdrop so the unit portraits read as character sheets.
		ShowBackdropColor(new Color(0.03f, 0.04f, 0.07f));

		_phaseLabel.Text = "UNIT ROSTER";
		_phaseLabel.Visible = true;
		_unitPortrait.Visible = true;
		SetPortraitSize(620f);

		for (_unitIndex = 0; _unitIndex < _unitRoster.Length; _unitIndex++)
		{
			var (texturePath, label) = _unitRoster[_unitIndex];
			var texture = GD.Load<Texture2D>(texturePath);
			_unitPortrait.Texture = texture;
			_phaseLabel.Text = label;

			await FadeIn(0.4f);
			await ToSignal(GetTree().CreateTimer(RosterSeconds), "timeout");
			await FadeOut(0.4f);
		}

		SetPortraitSize(400f);
		_unitPortrait.Visible = false;
		_phaseLabel.Visible = false;
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

	private async Task RunStrategicMapPhase()
	{
		_phaseLabel.Text = "STRATEGIC MAP";
		_phaseLabel.Visible = true;

		// The travel beat entered a facility, which makes the map open on that POI's facade.
		// The teaser wants the wide strategic view instead.
		MapNavigationContext.ClearReturnToFacade();

		var mapScene = GD.Load<PackedScene>("res://scenes/map.tscn");
		var mapInstance = mapScene.Instantiate();

		ClearCurrentScene();
		_currentSceneInstance = mapInstance;
		_viewport.GetNode("CurrentScene").AddChild(mapInstance);

		await FadeIn(1.0f);
		await ToSignal(GetTree().CreateTimer(MapSeconds), "timeout");
		_phaseLabel.Visible = false;
	}

	private async Task RunTitleCardPhase()
	{
		// The wordmark sits on the title art, clear of the phase caption at the bottom.
		LayoutTitleText();

		_titleText.Visible = true;
		_subtitleText.Visible = true;
		_titleScrim.Visible = true;

		var titleBg = GD.Load<Texture2D>("res://assets/story/pages/war-and-ruin2.png");
		var textureRect = new TextureRect
		{
			Texture = titleBg,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
			Modulate = new Color(1, 1, 1, 0.9f),
			Position = Vector2.Zero,
			Size = _viewport.Size,
		};

		ClearCurrentScene();
		_currentSceneInstance = textureRect;
		_viewport.GetNode("CurrentScene").AddChild(textureRect);

		await FadeIn(1.5f);
		await ToSignal(GetTree().CreateTimer(TitleCardSeconds), "timeout");

		_titleText.Visible = false;
		_subtitleText.Visible = false;
		_titleScrim.Visible = false;
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

	/// <summary>
	/// Removes whatever is currently staged in the viewport. Nodes are detached before they
	/// are freed so their canvases stop drawing on the very same frame.
	/// </summary>
	private void ClearCurrentScene()
	{
		var root = _viewport.GetNode<Node>("CurrentScene");
		foreach (var child in root.GetChildren())
		{
			root.RemoveChild(child);
			child.QueueFree();
		}

		_currentSceneInstance = null;
	}

	private async Task FadeIn(float duration)
	{
		_transition.Visible = true;
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

	private async Task FadeTransition(float duration)
	{
		await FadeOut(duration);
		await FadeIn(duration);
	}
}
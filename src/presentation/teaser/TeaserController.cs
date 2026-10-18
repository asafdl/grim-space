using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Godot;
using GrimSpace.Application;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Presentation.Scene;
using GrimSpace.Presentation.Intro;
using GrimSpace.Presentation.Menu;

namespace GrimSpace.Presentation.Teaser;

public partial class TeaserController : Node
{
	private enum TeaserPhase
	{
		StoryArtwork,
		TacticalCombat,
		SpaceTravel,
		UnitRoster,
		StrategicMap,
		TitleCard,
		Complete
	}

	private SubViewport _viewport = null!;
	private TextureRect _viewportDisplay = null!;
	Node? _currentSceneInstance = null;
	ColorRect _transition = null!;
	Label _titleText = null!;
	Label _subtitleText = null!;
	Label _phaseLabel = null!;
	TextureRect _unitPortrait = null!;

	private TeaserPhase _currentPhase = TeaserPhase.StoryArtwork;
	private int _storyPageIndex = 0;
	private readonly string[] _storyPages = new[]
	{
		"res://assets/story/pages/humanity-controls-galaxy.png",
		"res://assets/story/pages/war-and-ruin2.png",
		"res://assets/story/pages/total-war.png",
		"res://assets/story/pages/last-stand.png",
		"res://assets/story/pages/syndicate-attacks.png",
		"res://assets/story/pages/syndicate-fractures.png",
		"res://assets/story/pages/syndicate-empire.png",
		"res://assets/story/pages/syndicate-infection.png",
		"res://assets/story/pages/machine-emotion.png",
		"res://assets/story/pages/new-humanlike-civilization.png",
		"res://assets/story/pages/pilot-drunk.png",
		"res://assets/story/pages/humanity-loses-control.png",
		"res://assets/story/pages/intro-screen.png"
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

		if (Session.Instance.Run is null)
		{
			Session.Instance.StartNewRun();
		}

		StartPhase(TeaserPhase.TacticalCombat);
	}

	private async void StartPhase(TeaserPhase phase)
	{
		_currentPhase = phase;
		await FadeIn(0.5f);

		switch (phase)
		{
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

	private async Task RunStoryArtworkPhase()
	{
		_phaseLabel.Text = "THE FALL OF HUMANITY";
		_phaseLabel.Visible = true;

		for (_storyPageIndex = 0; _storyPageIndex < _storyPages.Length; _storyPageIndex++)
		{
			await ShowStoryPage(_storyPages[_storyPageIndex]);
			await ToSignal(GetTree().CreateTimer(3.0), "timeout");
			await FadeTransition(0.5f);
		}

		_phaseLabel.Visible = false;
	}

	private async Task ShowStoryPage(string texturePath)
	{
		var texture = GD.Load<Texture2D>(texturePath);
		var textureRect = new TextureRect
		{
			Texture = texture,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			AnchorsPreset = (int)Control.LayoutPreset.FullRect
		};

		ClearCurrentScene();
		_currentSceneInstance = textureRect;
		_viewport.GetNode("CurrentScene").AddChild(textureRect);

		await FadeIn(1.0f);
	}

	private async Task RunTacticalCombatPhase()
	{
		_phaseLabel.Text = "TACTICAL COMBAT";
		_phaseLabel.Visible = true;

		var encounter = AiVsAiRunner.CreateAiVsAiEncounter(seed: 99999);
		BattleController.NextEncounterOverride = encounter;

		var battleScene = GD.Load<PackedScene>("res://scenes/battle.tscn");
		var battleInstance = battleScene.Instantiate();

		ClearCurrentScene();
		_currentSceneInstance = battleInstance;
		_viewport.GetNode("CurrentScene").AddChild(battleInstance);

		await FadeIn(1.0f);

		var battleController = battleInstance.GetNode<BattleController>(".");
		
		// Wait one frame for BattleController._Ready() to create unit views, then set overview camera
		await ToSignal(GetTree(), "process_frame");
		GD.Print("[Teaser] About to call SetupOverviewCamera");
		battleController.SetupOverviewCamera();
		battleController.EnableStaticCameraMode();
		GD.Print("[Teaser] Static camera mode enabled - camera will not move during replay");
		GD.Print("[Teaser] Both calls completed");
		
		await battleController.RunAiVsAiAsync(maxTurns: 8, onTurnLog: (turn, msg) =>
		{
			GD.Print($"[Teaser Battle] Turn {turn}: {msg}");
		});

		await ToSignal(GetTree().CreateTimer(2.0), "timeout");
		_phaseLabel.Visible = false;
	}

	private async Task RunSpaceTravelPhase()
	{
		_phaseLabel.Text = "SPACE TRAVEL & FACILITIES";
		_phaseLabel.Visible = true;

		var travelScene = GD.Load<PackedScene>("res://scenes/travel.tscn");
		var travelInstance = travelScene.Instantiate();

		ClearCurrentScene();
		_currentSceneInstance = travelInstance;
		_viewport.GetNode("CurrentScene").AddChild(travelInstance);

		await FadeIn(1.0f);
		await ToSignal(GetTree().CreateTimer(8.0), "timeout");
		_phaseLabel.Visible = false;
	}

	private async Task RunUnitRosterPhase()
	{
		_phaseLabel.Text = "UNIT ROSTER";
		_phaseLabel.Visible = true;
		_unitPortrait.Visible = true;

		for (_unitIndex = 0; _unitIndex < _unitRoster.Length; _unitIndex++)
		{
			var (texturePath, label) = _unitRoster[_unitIndex];
			var texture = GD.Load<Texture2D>(texturePath);
			_unitPortrait.Texture = texture;
			_phaseLabel.Text = label;

			await FadeIn(0.5f);
			await ToSignal(GetTree().CreateTimer(2.5f), "timeout");
			await FadeOut(0.5f);
		}

		_unitPortrait.Visible = false;
		_phaseLabel.Visible = false;
	}

	private async Task RunStrategicMapPhase()
	{
		_phaseLabel.Text = "STRATEGIC MAP";
		_phaseLabel.Visible = true;

		var mapScene = GD.Load<PackedScene>("res://scenes/map.tscn");
		var mapInstance = mapScene.Instantiate();

		ClearCurrentScene();
		_currentSceneInstance = mapInstance;
		_viewport.GetNode("CurrentScene").AddChild(mapInstance);

		await FadeIn(1.0f);
		await ToSignal(GetTree().CreateTimer(8.0), "timeout");
		_phaseLabel.Visible = false;
	}

	private async Task RunTitleCardPhase()
	{
		_titleText.Visible = true;
		_subtitleText.Visible = true;

		var titleBg = GD.Load<Texture2D>("res://assets/story/pages/intro-screen.png");
		var textureRect = new TextureRect
		{
			Texture = titleBg,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			AnchorsPreset = (int)Control.LayoutPreset.FullRect,
			Modulate = new Color(1, 1, 1, 0.3f)
		};

		ClearCurrentScene();
		_currentSceneInstance = textureRect;
		_viewport.GetNode("CurrentScene").AddChild(textureRect);

		await FadeIn(2.0f);
		await ToSignal(GetTree().CreateTimer(5.0), "timeout");

		_titleText.Visible = false;
		_subtitleText.Visible = false;

		StartPhase(TeaserPhase.Complete);
	}

	private void ClearCurrentScene()
	{
		if (_currentSceneInstance != null && _currentSceneInstance.IsInsideTree())
		{
			_currentSceneInstance.QueueFree();
			_currentSceneInstance = null;
		}
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
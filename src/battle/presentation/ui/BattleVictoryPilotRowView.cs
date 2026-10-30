using Godot;
using GrimSpace.Components;
using GrimSpace.Run;
using GrimSpace.Ui.Components;
using GrimSpace.World.StarSystem.Presentation.Ui;

namespace GrimSpace.Battle.Presentation.Ui;

public sealed partial class BattleVictoryPilotRowView : Control
{
	private const float DefaultRowHeight = 110f;
	private const float BracketArtHeight = 300f;
	private const float BracketCircleCenterY = 136f;
	private const float PortraitDiameter = DefaultRowHeight * 245f / 300f;
	private const float PortraitGap = 0.2f;
	private const float PortraitOverlap = 18f;
	private const float XpBarHeight = 7f;
	private const float XpBarBottom = -11f;
	private const float XpBarRightInset = 28f;
	private const float XpBarWidthReduction = 328f;
	private const float LevelUpPauseSeconds = 0.55f;
	private const float ScoreTrailingHeight = 72f;
	private const float LevelUpIconSlotWidth = 36f;
	private const float FillPulseHalfSeconds = 0.16f;

	private readonly UiImageListItem _item;
	private readonly ProgressBar _xpBar;
	private readonly Label _levelLabel;
	private readonly Label _scrapStamp;
	private readonly PilotLevelUpChevron _levelUpIcon;
	private readonly bool _isScrapped;
	private readonly int _experienceBefore;
	private readonly int _experienceAfter;
	private int _lastAppliedXp;
	private int _displayedLevel;
	private Tween? _xpTween;
	private Tween? _trackFlashTween;
	private Tween? _fillPulseTween;
	private Tween? _scrapTween;

	public event Action? ExperienceFillFinished;

	public bool IsExperienceFillIdle => _xpTween is null && _scrapTween is null;

	public bool IsScrapped => _isScrapped;

	public BattleVictoryPilotRowView(BattleVictoryPilotRow row)
	{
		CustomMinimumSize = new Vector2(0f, DefaultRowHeight);
		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		MouseFilter = MouseFilterEnum.Ignore;

		_isScrapped = row.IsScrapped;
		_experienceBefore = row.ExperiencePointsBefore;
		_experienceAfter = row.ExperiencePointsAfter;
		_lastAppliedXp = row.ExperiencePointsBefore;
		_displayedLevel = PilotProgression.LevelFor(_experienceBefore);

		_levelUpIcon = new PilotLevelUpChevron
		{
			Visible = false,
			Pulse = true,
		};

		_item = new UiImageListItem(
			row.DisplayName,
			BattleHudCopy.VictoryPilotPerformanceMetadata(
				row.DamageDealt,
				row.DamageTaken,
				row.HealingDone,
				row.ExperienceEarned,
				row.IsScrapped),
			string.Empty,
			ShipPilotPortraitCatalog.TextureFor(row.PortraitId))
		{
			AnchorsPreset = (int)Control.LayoutPreset.FullRect,
			AnchorRight = 1f,
			AnchorBottom = 1f,
			FocusMode = FocusModeEnum.None,
			MouseFilter = MouseFilterEnum.Ignore,
			MouseDefaultCursorShape = CursorShape.Arrow,
		};
		_item.SetTrailingContent(CreateScoreTrailing(row.ScoreGrade, _levelUpIcon));
		AddChild(_item);

		_xpBar = PilotExperienceMeter.CreateBar();
		_xpBar.ZIndex = 2;
		_xpBar.Visible = !_isScrapped;
		AddChild(_xpBar);

		_levelLabel = new Label
		{
			Text = FormatLevel(_displayedLevel),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Top,
			MouseFilter = MouseFilterEnum.Ignore,
			ZIndex = 3,
			Visible = !_isScrapped,
		};
		_levelLabel.AddThemeFontSizeOverride("font_size", 13);
		_levelLabel.AddThemeColorOverride("font_color", Colors.White);
		_levelLabel.AddThemeColorOverride("font_outline_color", HudPalettes.BlueCyan.DeepSurface);
		_levelLabel.AddThemeConstantOverride("outline_size", 2);
		AddChild(_levelLabel);

		_scrapStamp = new Label
		{
			Text = BattleHudCopy.ScrappedStamp,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			MouseFilter = MouseFilterEnum.Ignore,
			ZIndex = 4,
			Visible = false,
			Modulate = new Color(1f, 1f, 1f, 0f),
		};
		_scrapStamp.AddThemeFontSizeOverride("font_size", 28);
		_scrapStamp.AddThemeColorOverride("font_color", new Color(0.95f, 0.28f, 0.22f));
		_scrapStamp.AddThemeColorOverride("font_outline_color", Colors.Black);
		_scrapStamp.AddThemeConstantOverride("outline_size", 4);
		AddChild(_scrapStamp);

		if (!_isScrapped)
			PilotExperienceMeter.ApplyTotalExperience(_xpBar, _experienceBefore);

		Resized += LayoutChrome;
		Callable.From(LayoutChrome).CallDeferred();
	}

	public void BeginRowReveal(float durationSeconds)
	{
		if (_isScrapped)
		{
			BeginScrapReveal(durationSeconds);
			return;
		}

		BeginExperienceFill(durationSeconds);
	}

	public void BeginExperienceFill(float durationSeconds)
	{
		_xpTween?.Kill();
		_trackFlashTween?.Kill();
		_trackFlashTween = null;
		StopFillPulse();
		PilotExperienceMeter.ResetTrackStyle(_xpBar);
		_lastAppliedXp = _experienceBefore;
		_displayedLevel = PilotProgression.LevelFor(_experienceBefore);
		_levelUpIcon.Visible = false;
		PilotExperienceMeter.ApplyTotalExperience(_xpBar, _experienceBefore);
		UpdateLevelLabel(force: true);

		if (_experienceAfter <= _experienceBefore || durationSeconds <= 0f)
		{
			ApplyInstantExperience(_experienceAfter);
			NotifyExperienceFillFinished();
			return;
		}

		StartFillPulse();
		_xpTween = CreateTween();
		AppendExperienceSegments(_xpTween, _experienceBefore, _experienceAfter, durationSeconds);
		_xpTween.Finished += () =>
		{
			_xpTween = null;
			StopFillPulse();
			UpdateLevelLabel(force: true);
			NotifyExperienceFillFinished();
		};
	}

	private void NotifyExperienceFillFinished() => ExperienceFillFinished?.Invoke();

	private static Control CreateScoreTrailing(char grade, PilotLevelUpChevron levelUpIcon)
	{
		var trailing = new HBoxContainer
		{
			Alignment = BoxContainer.AlignmentMode.Center,
			MouseFilter = MouseFilterEnum.Ignore,
			CustomMinimumSize = new Vector2(72f + LevelUpIconSlotWidth, ScoreTrailingHeight),
		};
		trailing.AddThemeConstantOverride("separation", 0);

		var gradeLabel = BattlePerformanceGradeDisplay.CreateTrailing(grade);
		gradeLabel.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		trailing.AddChild(gradeLabel);

		var iconSlot = new CenterContainer
		{
			CustomMinimumSize = new Vector2(LevelUpIconSlotWidth, ScoreTrailingHeight),
			SizeFlagsVertical = SizeFlags.ShrinkCenter,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		levelUpIcon.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
		levelUpIcon.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		iconSlot.AddChild(levelUpIcon);
		trailing.AddChild(iconSlot);

		return trailing;
	}

	private void AppendExperienceSegments(Tween tween, int from, int to, float totalDuration)
	{
		var totalDelta = to - from;
		var cursor = from;
		while (cursor < to)
		{
			var segmentEnd = to;
			var endsAtLevelUp = false;
			if (PilotProgression.TryGetNextLevelThreshold(cursor, out var threshold) && to >= threshold)
			{
				segmentEnd = threshold;
				endsAtLevelUp = true;
			}

			var segmentStart = (float)cursor;
			var fillEndXp = endsAtLevelUp ? segmentEnd - 1 : segmentEnd;
			var segmentDuration = totalDuration * (segmentEnd - cursor) / (float)totalDelta;
			tween.TweenMethod(
					Callable.From<float>(value => ApplySegmentFill(value, fillEndXp)),
					segmentStart,
					(float)fillEndXp,
					segmentDuration)
				.SetTrans(Tween.TransitionType.Expo)
				.SetEase(Tween.EaseType.Out);

			if (endsAtLevelUp)
			{
				var levelThreshold = segmentEnd;
				tween.TweenCallback(Callable.From(() => CommitLevelUp(levelThreshold)));
				tween.TweenInterval(LevelUpPauseSeconds);
				tween.TweenCallback(Callable.From(() => BeginNextLevelSegment(levelThreshold)));
			}

			cursor = segmentEnd;
		}
	}

	private void ApplySegmentFill(float value, int segmentCapXp)
	{
		var xp = System.Math.Min((int)Mathf.Round(value), segmentCapXp);

		if (xp == _lastAppliedXp)
			return;

		PilotExperienceMeter.ApplyTotalExperience(_xpBar, xp);
		_lastAppliedXp = xp;
		UpdateLevelLabel();
	}

	private void CommitLevelUp(int thresholdExperience)
	{
		var completedLevel = PilotProgression.LevelFor(thresholdExperience - 1);
		PilotExperienceMeter.ApplyCompletedLevelSegment(_xpBar, completedLevel);
		BattleVictoryUiSfx.PlayLevelUpSlam(this);
		_levelUpIcon.Visible = true;
		_trackFlashTween?.Kill();
		_trackFlashTween = PilotExperienceMeter.FlashLevelUpBorder(_xpBar, this);
		_trackFlashTween.Finished += () => _trackFlashTween = null;
		_lastAppliedXp = thresholdExperience;
		UpdateLevelLabel(force: true);
	}

	private void BeginNextLevelSegment(int thresholdExperience)
	{
		PilotExperienceMeter.ApplyTotalExperience(_xpBar, thresholdExperience);
		_lastAppliedXp = thresholdExperience;
		UpdateLevelLabel(force: true);
	}

	private void ApplyInstantExperience(int targetXp)
	{
		var cursor = _lastAppliedXp;
		while (cursor < targetXp)
		{
			if (PilotProgression.TryGetNextLevelThreshold(cursor, out var threshold) && targetXp >= threshold)
			{
				PilotExperienceMeter.ApplyTotalExperience(_xpBar, threshold - 1);
				_lastAppliedXp = threshold - 1;
				CommitLevelUp(threshold);
				BeginNextLevelSegment(threshold);
				cursor = threshold;
				continue;
			}

			break;
		}

		PilotExperienceMeter.ApplyTotalExperience(_xpBar, targetXp);
		_lastAppliedXp = targetXp;
		UpdateLevelLabel(force: true);
	}

	private void UpdateLevelLabel(bool force = false)
	{
		var level = PilotProgression.LevelFor(_lastAppliedXp);
		if (!force && level == _displayedLevel)
			return;

		_displayedLevel = level;
		_levelLabel.Text = FormatLevel(level);
	}

	private static string FormatLevel(int level) => $"LV {level}";

	private void StartFillPulse()
	{
		_fillPulseTween?.Kill();
		_fillPulseTween = CreateTween().SetLoops();
		_fillPulseTween.TweenProperty(
				_xpBar,
				"modulate",
				new Color(1.55f, 1.35f, 1.4f),
				FillPulseHalfSeconds)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
		_fillPulseTween.TweenProperty(
				_xpBar,
				"modulate",
				Colors.White,
				FillPulseHalfSeconds)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
	}

	private void StopFillPulse()
	{
		_fillPulseTween?.Kill();
		_fillPulseTween = null;
		_xpBar.Modulate = Colors.White;
	}

	public void CancelExperienceFill()
	{
		_xpTween?.Kill();
		_xpTween = null;
		_trackFlashTween?.Kill();
		_trackFlashTween = null;
		_scrapTween?.Kill();
		_scrapTween = null;
		StopFillPulse();
		PilotExperienceMeter.ResetTrackStyle(_xpBar);
		_levelUpIcon.Visible = false;
		_scrapStamp.Visible = false;
		_scrapStamp.Modulate = new Color(1f, 1f, 1f, 0f);
		_scrapStamp.Scale = Vector2.One;
	}

	private void BeginScrapReveal(float durationSeconds)
	{
		_scrapTween?.Kill();
		_scrapStamp.Visible = true;
		_scrapStamp.Modulate = new Color(1f, 1f, 1f, 0f);
		_scrapStamp.Scale = new Vector2(1.8f, 1.8f);
		LayoutChrome();

		var beat = Mathf.Max(0.35f, durationSeconds * 0.45f);
		_scrapTween = CreateTween();
		_scrapTween.SetParallel(true);
		_scrapTween.TweenProperty(_scrapStamp, "modulate:a", 1f, beat * 0.35f)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.Out);
		_scrapTween.TweenProperty(_scrapStamp, "scale", Vector2.One, beat)
			.SetTrans(Tween.TransitionType.Back)
			.SetEase(Tween.EaseType.Out);
		_scrapTween.SetParallel(false);
		_scrapTween.TweenInterval(0.2f);
		_scrapTween.Finished += () =>
		{
			_scrapTween = null;
			NotifyExperienceFillFinished();
		};
	}

	private void LayoutChrome()
	{
		if (Size.X <= 0f || Size.Y <= 0f)
			return;

		var bodyWidth = Mathf.Max(
			0f,
			Size.X - PortraitGap - PortraitDiameter + PortraitOverlap);
		var barWidth = Mathf.Max(48f, bodyWidth - XpBarWidthReduction);
		var barLeft = bodyWidth - XpBarRightInset - barWidth;
		_xpBar.Position = new Vector2(barLeft, Size.Y - XpBarBottom - XpBarHeight);
		_xpBar.Size = new Vector2(barWidth, XpBarHeight);

		var portraitLeft = bodyWidth + PortraitGap - PortraitOverlap;
		var radius = PortraitDiameter * 0.5f;
		var portraitCenterY = (Size.Y - DefaultRowHeight) * 0.5f
			+ DefaultRowHeight * BracketCircleCenterY / BracketArtHeight;
		_levelLabel.Position = new Vector2(portraitLeft, portraitCenterY - radius + 2f);
		_levelLabel.Size = new Vector2(PortraitDiameter, 20f);

		_scrapStamp.PivotOffset = new Vector2(bodyWidth * 0.5f, Size.Y * 0.5f);
		_scrapStamp.Position = Vector2.Zero;
		_scrapStamp.Size = new Vector2(bodyWidth, Size.Y);
	}
}

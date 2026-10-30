using Godot;
using GrimSpace.Components;
using GrimSpace.Run;

namespace GrimSpace.Battle.Presentation.Ui;

internal static class PilotExperienceMeter
{
	private static readonly Color TrackBackground =
		HudPalettes.WithAlpha(HudPalettes.BlueCyan.Surface, 0.95f);
	private static readonly Color FillColor = HudPalettes.PurpleRed.Secondary;
	private const float LevelUpBorderFlashSeconds = 0.55f;

	public static ProgressBar CreateBar()
	{
		var bar = new ProgressBar
		{
			CustomMinimumSize = new Vector2(0f, 7f),
			ShowPercentage = false,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		bar.AddThemeStyleboxOverride("fill", FillStyle(goldBorder: false));
		bar.AddThemeStyleboxOverride("background", TrackStyle());
		return bar;
	}

	public static Tween FlashLevelUpBorder(ProgressBar bar, Node host)
	{
		// Border must be on fill — at 100% the fill covers the track background.
		bar.AddThemeStyleboxOverride("fill", FillStyle(goldBorder: true));
		var tween = host.CreateTween();
		tween.TweenInterval(LevelUpBorderFlashSeconds);
		tween.TweenCallback(Callable.From(() =>
			bar.AddThemeStyleboxOverride("fill", FillStyle(goldBorder: false))));
		return tween;
	}

	public static void ResetTrackStyle(ProgressBar bar) =>
		bar.AddThemeStyleboxOverride("fill", FillStyle(goldBorder: false));

	public static void ApplyTotalExperience(ProgressBar bar, int experiencePoints)
	{
		var (currentInLevel, requiredForNext) = PilotProgression.ProgressWithinLevel(experiencePoints);
		if (requiredForNext <= 0)
		{
			bar.MaxValue = 1;
			bar.Value = 1;
			return;
		}

		bar.MaxValue = requiredForNext;
		bar.Value = Mathf.Clamp(currentInLevel, 0, requiredForNext);
	}

	public static void ApplyCompletedLevelSegment(ProgressBar bar, int completedLevel)
	{
		if (completedLevel >= PilotProgression.MaxLevel)
		{
			bar.MaxValue = 1;
			bar.Value = 1;
			return;
		}

		var levelStart = PilotProgression.ExperienceForLevel(completedLevel);
		var nextLevelStart = PilotProgression.ExperienceForLevel(completedLevel + 1);
		bar.MaxValue = nextLevelStart - levelStart;
		bar.Value = bar.MaxValue;
	}

	private static StyleBoxFlat FillStyle(bool goldBorder)
	{
		var style = new StyleBoxFlat
		{
			BgColor = FillColor,
			CornerRadiusTopLeft = 2,
			CornerRadiusTopRight = 2,
			CornerRadiusBottomLeft = 2,
			CornerRadiusBottomRight = 2,
		};
		if (!goldBorder)
			return style;

		style.BorderWidthTop = 2;
		style.BorderWidthBottom = 2;
		style.BorderWidthLeft = 2;
		style.BorderWidthRight = 2;
		style.BorderColor = HudPalettes.Gold.Primary;
		return style;
	}

	private static StyleBoxFlat TrackStyle() =>
		new()
		{
			BgColor = TrackBackground,
			CornerRadiusTopLeft = 2,
			CornerRadiusTopRight = 2,
			CornerRadiusBottomLeft = 2,
			CornerRadiusBottomRight = 2,
		};
}

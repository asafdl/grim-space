using Godot;
using GrimSpace.Components;

namespace GrimSpace.Battle.Presentation.Ui;

internal static class BattlePerformanceGradeDisplay
{
	private const string ScoreFontPath = "res://assets/ui/fonts/AnakManja.ttf";
	private const int ScoreFontSize = 56;

	private static FontFile? _scoreFont;

	public static Control CreateTrailing(char grade)
	{
		var label = new Label
		{
			Text = grade.ToString(),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			CustomMinimumSize = new Vector2(72f, 72f),
		};
		label.AddThemeFontOverride("font", ScoreFont());
		label.AddThemeFontSizeOverride("font_size", ScoreFontSize);
		label.AddThemeColorOverride("font_color", ColorFor(grade));
		return label;
	}

	private static Color ColorFor(char grade) =>
		grade switch
		{
			'S' => HudPalettes.Gold.Highlight,
			'A' => HudPalettes.Gold.Primary,
			'B' => HudPalettes.Gold.Secondary,
			'C' => HudPalettes.WithAlpha(HudPalettes.Gold.Border, 0.95f),
			_ => HudPalettes.Gold.Secondary,
		};

	private static FontFile ScoreFont() =>
		_scoreFont ??= GD.Load<FontFile>(ScoreFontPath)
		?? throw new InvalidOperationException($"Could not load score font '{ScoreFontPath}'.");
}

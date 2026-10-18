using Godot;

namespace GrimSpace.Battle.Presentation.Ui;

public sealed partial class BattleCommandConsole : MarginContainer
{
	private static readonly Color MainFill = new(0.055f, 0.045f, 0.07f, 0.98f);
	private static readonly Color DeepFill = new(0.02f, 0.02f, 0.035f, 0.99f);
	private static readonly Color WingFill = new(0.105f, 0.075f, 0.13f, 0.98f);
	private static readonly Color EdgeColor = new(0.63f, 0.57f, 0.66f, 0.92f);
	private static readonly Color HighlightColor = new(0.84f, 0.79f, 0.86f, 0.62f);
	private static readonly Color InnerEdgeColor = new(0.37f, 0.3f, 0.42f, 0.72f);
	private static readonly Color RailGlowColor = new(0.72f, 0.58f, 0.78f, 0.13f);

	public BattleCommandConsole()
	{
		CustomMinimumSize = new Vector2(0f, 120f);
		MouseFilter = MouseFilterEnum.Ignore;
		AddThemeConstantOverride("margin_left", 18);
		AddThemeConstantOverride("margin_top", 24);
		AddThemeConstantOverride("margin_right", 18);
		AddThemeConstantOverride("margin_bottom", 12);
	}

	public override void _Draw()
	{
		var width = Size.X;
		var height = Size.Y;
		var wingLeft = width - 190f;
		var mainTop = 22f;
		var wingTop = 4f;

		Vector2[] shell =
		[
			new(0f, height),
			new(0f, mainTop + 12f),
			new(12f, mainTop),
			new(wingLeft - 16f, mainTop),
			new(wingLeft, wingTop),
			new(width - 10f, wingTop),
			new(width, wingTop + 10f),
			new(width, height),
		];
		Color[] shellColors =
		[
			DeepFill,
			MainFill,
			MainFill,
			MainFill,
			MainFill.Lightened(0.08f),
			MainFill.Lightened(0.08f),
			MainFill,
			DeepFill,
		];
		DrawPolygon(shell, shellColors);

		Vector2[] wing =
		[
			new(wingLeft, wingTop),
			new(width - 10f, wingTop),
			new(width, wingTop + 10f),
			new(width, height),
			new(wingLeft, height),
		];
		Color[] wingColors =
		[
			WingFill.Lightened(0.08f),
			WingFill.Lightened(0.08f),
			WingFill,
			WingFill.Darkened(0.55f),
			WingFill.Darkened(0.55f),
		];
		DrawPolygon(wing, wingColors);

		DrawPolyline(shell, EdgeColor, 1.5f, true);
		Vector2[] innerEdge =
		[
			new(5f, height),
			new(5f, mainTop + 15f),
			new(14f, mainTop + 5f),
			new(wingLeft - 13f, mainTop + 5f),
			new(wingLeft + 3f, wingTop + 5f),
			new(width - 12f, wingTop + 5f),
			new(width - 5f, wingTop + 12f),
			new(width - 5f, height),
		];
		DrawPolyline(innerEdge, InnerEdgeColor, 1f, true);
		DrawLine(
			new Vector2(wingLeft, wingTop),
			new Vector2(wingLeft, height),
			EdgeColor,
			1.2f,
			true);
		DrawLine(
			new Vector2(wingLeft + 5f, wingTop + 8f),
			new Vector2(wingLeft + 5f, height),
			InnerEdgeColor,
			1f,
			true);
		DrawLine(
			new Vector2(14f, mainTop),
			new Vector2(wingLeft - 18f, mainTop),
			RailGlowColor,
			6f,
			true);
		DrawLine(
			new Vector2(14f, mainTop),
			new Vector2(wingLeft - 18f, mainTop),
			HighlightColor,
			1f,
			true);
		DrawLine(
			new Vector2(wingLeft + 3f, wingTop),
			new Vector2(width - 12f, wingTop),
			RailGlowColor,
			6f,
			true);
		DrawLine(
			new Vector2(wingLeft + 3f, wingTop),
			new Vector2(width - 12f, wingTop),
			HighlightColor,
			1f,
			true);

		for (var y = wingTop + 18f; y < height - 8f; y += 9f)
			DrawLine(
				new Vector2(width - 18f, y),
				new Vector2(width - 8f, y + 10f),
				InnerEdgeColor,
				1f,
				true);

		for (var y = mainTop + 22f; y < height - 8f; y += 10f)
			DrawLine(
				new Vector2(1f, y),
				new Vector2(5f, y),
				HighlightColor,
				1f,
				true);
	}

	public override void _Notification(int what)
	{
		if (what == (int)NotificationResized || what == (int)NotificationThemeChanged)
			QueueRedraw();
	}
}

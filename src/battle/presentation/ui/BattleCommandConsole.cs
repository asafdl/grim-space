using Godot;
using GrimSpace.Components;

namespace GrimSpace.Battle.Presentation.Ui;

public sealed partial class BattleCommandConsole : MarginContainer
{
	private const float DefaultWingContentWidth = 172f;
	private static readonly Color MainFill =
		HudPalettes.WithAlpha(HudPalettes.PurpleRed.Surface, 0.98f);
	private static readonly Color DeepFill =
		HudPalettes.WithAlpha(HudPalettes.PurpleRed.DeepSurface, 0.99f);
	private static readonly Color WingFill =
		HudPalettes.WithAlpha(HudPalettes.PurpleRed.Surface.Lightened(0.05f), 0.98f);
	private static readonly Color EdgeColor =
		HudPalettes.WithAlpha(HudPalettes.PurpleRed.Primary, 0.92f);
	private static readonly Color HighlightColor =
		HudPalettes.WithAlpha(HudPalettes.PurpleRed.Primary.Lightened(0.18f), 0.62f);
	private static readonly Color InnerEdgeColor =
		HudPalettes.WithAlpha(HudPalettes.PurpleRed.Border, 0.72f);
	private static readonly Color RailGlowColor =
		HudPalettes.WithAlpha(HudPalettes.PurpleRed.Primary, 0.13f);
	private float _wingContentWidth = DefaultWingContentWidth;

	public float HorizontalContentMargins =>
		GetThemeConstant("margin_left") + GetThemeConstant("margin_right");

	public BattleCommandConsole()
	{
		CustomMinimumSize = new Vector2(0f, 160f);
		MouseFilter = MouseFilterEnum.Ignore;
		AddThemeConstantOverride("margin_left", 18);
		AddThemeConstantOverride("margin_top", 24);
		AddThemeConstantOverride("margin_right", 18);
		AddThemeConstantOverride("margin_bottom", 12);
	}

	public void SetWingContentWidth(float width)
	{
		if (width <= 0f)
			throw new ArgumentOutOfRangeException(nameof(width));
		if (Mathf.IsEqualApprox(_wingContentWidth, width))
			return;

		_wingContentWidth = width;
		QueueRedraw();
	}

	public override void _Draw()
	{
		var width = Size.X;
		var height = Size.Y;
		var wingLeft = width - GetThemeConstant("margin_right") - _wingContentWidth;
		var mainTop = 22f;
		var wingTop = 4f;
		var sideSlope = 18f;
		const float leftArcWidth = 120f;
		const int arcPointCount = 16;
		var leftArcCenter = new Vector2(-leftArcWidth, mainTop);
		var leftArcHeight = height - mainTop;

		var shell = new List<Vector2>
		{
			EllipseArcPoint(0f, leftArcCenter, leftArcWidth, leftArcHeight),
		};
		var shellColors = new List<Color> { DeepFill };
		for (var i = 1; i <= arcPointCount; i++)
		{
			var progress = i / (float)arcPointCount;
			shell.Add(EllipseArcPoint(
				progress,
				leftArcCenter,
				leftArcWidth,
				leftArcHeight));
			shellColors.Add(DeepFill.Lerp(MainFill, progress));
		}
		shell.Add(new Vector2(wingLeft - 16f, mainTop));
		shell.Add(new Vector2(wingLeft, wingTop));
		shell.Add(new Vector2(width - sideSlope, wingTop));
		shell.Add(new Vector2(width, height));
		shellColors.Add(MainFill);
		shellColors.Add(MainFill.Lightened(0.08f));
		shellColors.Add(MainFill.Lightened(0.08f));
		shellColors.Add(DeepFill);
		DrawPolygon(shell.ToArray(), shellColors.ToArray());

		Vector2[] wing =
		[
			new(wingLeft, wingTop),
			new(width - sideSlope, wingTop),
			new(width, height),
			new(wingLeft, height),
		];
		Color[] wingColors =
		[
			WingFill.Lightened(0.08f),
			WingFill.Lightened(0.08f),
			WingFill,
			WingFill.Darkened(0.55f),
		];
		DrawPolygon(wing, wingColors);

		const float innerInset = 5f;
		var outerEdge = shell
			.Skip(arcPointCount)
			.Append(shell[0])
			.ToArray();
		DrawPolyline(outerEdge, EdgeColor, 1.5f, false);
		DrawEllipseArc(
			leftArcCenter,
			leftArcWidth,
			leftArcHeight,
			0f,
			Mathf.Pi / 2f,
			arcPointCount,
			EdgeColor,
			1.5f,
			true);

		var innerArcCenter = new Vector2(
			-leftArcWidth + innerInset,
			mainTop + innerInset);
		var innerArcHeight = height - mainTop - innerInset * 2f;
		var innerEdge = new List<Vector2>
		{
			EllipseArcPoint(0f, innerArcCenter, leftArcWidth, innerArcHeight),
		};
		for (var i = 1; i <= arcPointCount; i++)
		{
			innerEdge.Add(EllipseArcPoint(
				i / (float)arcPointCount,
				innerArcCenter,
				leftArcWidth,
				innerArcHeight));
		}
		innerEdge.Add(new Vector2(wingLeft - 13f, mainTop + 5f));
		innerEdge.Add(new Vector2(wingLeft + 3f, wingTop + 5f));
		innerEdge.Add(new Vector2(width - sideSlope - 2f, wingTop + 5f));
		innerEdge.Add(new Vector2(width - 5f, height));
		DrawPolyline(innerEdge.ToArray(), InnerEdgeColor, 1f, true);
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
			new Vector2(0f, mainTop),
			new Vector2(wingLeft - 18f, mainTop),
			RailGlowColor,
			6f,
			true);
		DrawLine(
			new Vector2(0f, mainTop),
			new Vector2(wingLeft - 18f, mainTop),
			HighlightColor,
			1f,
			true);
		DrawLine(
			new Vector2(wingLeft + 3f, wingTop),
			new Vector2(width - sideSlope, wingTop),
			RailGlowColor,
			6f,
			true);
		DrawLine(
			new Vector2(wingLeft + 3f, wingTop),
			new Vector2(width - sideSlope, wingTop),
			HighlightColor,
			1f,
			true);

		for (var y = wingTop + 18f; y < height - 8f; y += 9f)
		{
			var edgeX = width - sideSlope + sideSlope * ((y - wingTop) / (height - wingTop));
			DrawLine(
				new Vector2(edgeX - 13f, y),
				new Vector2(edgeX - 5f, y + 8f),
				InnerEdgeColor,
				1f,
				true);
		}

	}

	private static Vector2 EllipseArcPoint(
		float progress,
		Vector2 center,
		float radiusX,
		float radiusY)
	{
		var angle = Mathf.Lerp(Mathf.Pi / 2f, 0f, progress);
		return center + new Vector2(
			radiusX * Mathf.Cos(angle),
			radiusY * Mathf.Sin(angle));
	}

	public override void _Notification(int what)
	{
		if (what == (int)NotificationResized || what == (int)NotificationThemeChanged)
			QueueRedraw();
	}
}

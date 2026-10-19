using Godot;

namespace GrimSpace.Battle.Presentation.Ui;

internal static class ProjectedDirectionHandleRenderer
{
	public const float HitRadius = 28f;

	private const float ShellRadius = 50f;
	private const float ChevronDepth = 23f;
	private const float ChevronHalfWidth = 17f;
	private const float EmphasisOffset = 7f;
	private const float DepthHandleRadius = 18f;

	private static readonly Color DisabledColor = new(0.3f, 0.33f, 0.37f, 0.48f);
	private static readonly Color ShellCenter = new(0.025f, 0.045f, 0.065f, 0.04f);
	private static readonly Color ShellEdge = new(0.025f, 0.045f, 0.065f, 0.62f);
	private static readonly Color OutlineColor = new(0.78f, 0.88f, 0.96f, 0.98f);
	private static readonly Color Shadow = new(0f, 0f, 0f, 0.72f);

	public static void DrawShell(CanvasItem canvas, Vector2 center)
	{
		var shell = RegularPolygon(center, ShellRadius, 6, -Mathf.Pi / 6f);
		for (var i = 0; i < shell.Length; i++)
		{
			Vector2[] fadeTriangle =
				[center, shell[i], shell[(i + 1) % shell.Length]];
			canvas.DrawPolygon(fadeTriangle, [ShellCenter, ShellEdge, ShellEdge]);
		}
		Vector2[] outline = [.. shell, shell[0]];
		canvas.DrawPolyline(outline, Shadow, 8f, true);
		canvas.DrawPolyline(outline, OutlineColor, 3.5f, true);
	}

	public static void Draw(
		CanvasItem canvas,
		Vector2 position,
		float rotation,
		MovementSelection.HeadingHandleKind kind,
		Color color,
		bool enabled,
		bool emphasized,
		float progress = 0f)
	{
		color = enabled ? color : DisabledColor;
		if (emphasized && enabled)
			color = color.Lerp(Colors.White, 0.28f);

		if (kind == MovementSelection.HeadingHandleKind.Arrow)
		{
			DrawChevron(canvas, position, rotation, color, enabled, emphasized, progress);
			return;
		}

		DrawDepthHandle(canvas, position, kind, color, enabled, emphasized, progress);
	}

	private static void DrawChevron(
		CanvasItem canvas,
		Vector2 position,
		float rotation,
		Color color,
		bool enabled,
		bool emphasized,
		float progress)
	{
		var direction = Vector2.FromAngle(rotation);
		var tangent = new Vector2(-direction.Y, direction.X);
		var center = position + direction * (emphasized ? EmphasisOffset : 0f);
		var baseCenter = center - direction * (ChevronDepth * 0.5f);
		var apex = center + direction * (ChevronDepth * 0.5f);
		Vector2[] points =
		[
			baseCenter + tangent * ChevronHalfWidth,
			apex,
			baseCenter - tangent * ChevronHalfWidth,
		];
		if (enabled)
		{
			var glow = new Color(color.R, color.G, color.B, emphasized ? 0.58f : 0.32f);
			canvas.DrawPolyline(points, Shadow, emphasized ? 13f : 10f, true);
			canvas.DrawPolyline(points, glow, emphasized ? 11f : 8f, true);
		}
		canvas.DrawPolyline(
			points,
			color,
			enabled ? emphasized ? 7f : 5f : 3f,
			true);

		if (progress <= 0f)
			return;

		var end = baseCenter.Lerp(apex, System.Math.Clamp(progress, 0f, 1f));
		canvas.DrawLine(baseCenter, end, Shadow, 7f, true);
		canvas.DrawLine(
			baseCenter,
			end,
			color.Lerp(Colors.White, 0.35f),
			4f,
			true);
	}

	private static void DrawDepthHandle(
		CanvasItem canvas,
		Vector2 position,
		MovementSelection.HeadingHandleKind kind,
		Color color,
		bool enabled,
		bool emphasized,
		float progress)
	{
		var radius = DepthHandleRadius + (emphasized ? 3f : 0f);
		canvas.DrawCircle(position + new Vector2(3f, 3f), radius, Shadow);
		canvas.DrawCircle(position, radius, color);
		canvas.DrawArc(
			position,
			radius,
			0f,
			Mathf.Tau,
			32,
			enabled ? OutlineColor : DisabledColor,
			emphasized && enabled ? 4f : 2f,
			true);
		if (progress > 0f)
		{
			canvas.DrawArc(
				position,
				radius - 3f,
				-Mathf.Pi / 2f,
				-Mathf.Pi / 2f + Mathf.Tau * System.Math.Clamp(progress, 0f, 1f),
				32,
				color.Lerp(Colors.White, 0.35f),
				5f,
				true);
		}

		if (kind == MovementSelection.HeadingHandleKind.TowardCamera)
		{
			canvas.DrawCircle(position, 5f, OutlineColor);
			return;
		}

		canvas.DrawLine(
			position + new Vector2(-7f, -7f),
			position + new Vector2(7f, 7f),
			OutlineColor,
			3f,
			true);
		canvas.DrawLine(
			position + new Vector2(-7f, 7f),
			position + new Vector2(7f, -7f),
			OutlineColor,
			3f,
			true);
	}

	private static Vector2[] RegularPolygon(
		Vector2 center,
		float radius,
		int pointCount,
		float startAngle) =>
		Enumerable.Range(0, pointCount)
			.Select(index =>
				center + Vector2.FromAngle(
					startAngle + Mathf.Tau * index / pointCount) * radius)
			.ToArray();
}

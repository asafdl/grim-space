using Godot;

namespace GrimSpace.Battle.Presentation.Ui;

/// <summary>Chase trail around an ability slot border — ready-to-commit affordance.</summary>
public sealed partial class AbilityCommitTrail : Control
{
	private const float PeriodSeconds = 1.15f;
	private const float TrailFraction = 0.28f;
	private const int SampleCount = 64;
	private const float LineWidth = 2.4f;
	private const float Inset = 3f;

	private static readonly Color HeadColor = new(1f, 0.94f, 0.72f, 1f);
	private static readonly Color TailColor = new(1f, 0.78f, 0.28f, 0f);

	private float _phase;
	private bool _active;

	public AbilityCommitTrail()
	{
		Name = "CommitTrail";
		MouseFilter = MouseFilterEnum.Ignore;
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		GrowHorizontal = GrowDirection.Both;
		GrowVertical = GrowDirection.Both;
		Visible = false;
		SetProcess(false);
	}

	public void SetActive(bool active)
	{
		if (_active == active)
			return;

		_active = active;
		Visible = active;
		SetProcess(active);
		if (!active)
			_phase = 0f;
		QueueRedraw();
	}

	public override void _Process(double delta)
	{
		_phase = (_phase + (float)(delta / PeriodSeconds)) % 1f;
		QueueRedraw();
	}

	public override void _Draw()
	{
		if (!_active || Size.X < 4f || Size.Y < 4f)
			return;

		var rect = new Rect2(new Vector2(Inset, Inset), Size - new Vector2(Inset * 2f, Inset * 2f));
		var perimeter = 2f * (rect.Size.X + rect.Size.Y);
		if (perimeter <= 0f)
			return;

		var trailLength = perimeter * TrailFraction;
		var head = _phase * perimeter;

		Vector2? previous = null;
		for (var i = 0; i <= SampleCount; i++)
		{
			var alongTrail = trailLength * (i / (float)SampleCount);
			var distance = (head - alongTrail + perimeter) % perimeter;
			var point = PointOnRect(rect, distance);
			if (previous is { } from)
			{
				var t = i / (float)SampleCount;
				var color = HeadColor.Lerp(TailColor, t);
				DrawLine(from, point, color, LineWidth, antialiased: true);
			}

			previous = point;
		}
	}

	private static Vector2 PointOnRect(Rect2 rect, float distance)
	{
		var w = rect.Size.X;
		var h = rect.Size.Y;
		var d = distance % (2f * (w + h));
		var left = rect.Position.X;
		var top = rect.Position.Y;
		var right = left + w;
		var bottom = top + h;

		if (d < w)
			return new Vector2(left + d, top);
		d -= w;
		if (d < h)
			return new Vector2(right, top + d);
		d -= h;
		if (d < w)
			return new Vector2(right - d, bottom);
		d -= w;
		return new Vector2(left, bottom - d);
	}
}

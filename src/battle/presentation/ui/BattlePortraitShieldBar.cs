using Godot;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Loadouts.Defenses;

namespace GrimSpace.Battle.Presentation.Ui;

public sealed partial class BattlePortraitShieldBar : Control
{
	private const float InnerRadius = 66f;
	private const float OuterRadius = 87f;
	private const float StartAngle = Mathf.Pi * 0.72f;
	private const float EndAngle = Mathf.Pi * 1.69f;
	private const float SegmentGap = Mathf.Pi * 0.012f;
	private const int ArcPointCount = 10;

	private static readonly Color EmptyColor = new(0.025f, 0.08f, 0.14f, 0.96f);
	private static readonly Color FillColor = new(0.12f, 0.58f, 0.94f, 0.95f);
	private static readonly Color BorderColor = new(0.68f, 0.86f, 1f, 0.96f);
	private static readonly ESpatialOrientation[] Faces =
	[
		ESpatialOrientation.Forward,
		ESpatialOrientation.Retro,
		ESpatialOrientation.Starboard,
		ESpatialOrientation.Port,
		ESpatialOrientation.Dorsal,
		ESpatialOrientation.Ventral,
	];

	private readonly int[] _current = new int[Faces.Length];
	private readonly int[] _max = new int[Faces.Length];

	public BattlePortraitShieldBar()
	{
		CustomMinimumSize = new Vector2(166f, 166f);
		MouseFilter = MouseFilterEnum.Stop;
		TooltipText = BattleHudCopy.ShieldsTooltip;
	}

	public void Set(FaceShieldPoints current, FaceShieldPoints max)
	{
		Visible = max.MaxOnAnyFace > 0;
		for (var i = 0; i < Faces.Length; i++)
		{
			_max[i] = System.Math.Max(0, max[Faces[i]]);
			_current[i] = System.Math.Clamp(current[Faces[i]], 0, _max[i]);
		}
		QueueRedraw();
	}

	public override bool _HasPoint(Vector2 point) =>
		TryGetFaceIndex(point, out _);

	public override string _GetTooltip(Vector2 atPosition)
	{
		if (!TryGetFaceIndex(atPosition, out var faceIndex))
			return string.Empty;

		return $"{BattleHudCopy.FaceName(Faces[faceIndex])} Shields "
			+ $"{_current[faceIndex]}/{_max[faceIndex]}";
	}

	public override void _Draw()
	{
		var center = Size * 0.5f;
		var segmentSpan = (EndAngle - StartAngle) / Faces.Length;
		for (var i = 0; i < Faces.Length; i++)
		{
			var start = StartAngle + segmentSpan * i + SegmentGap * 0.5f;
			var end = StartAngle + segmentSpan * (i + 1) - SegmentGap * 0.5f;
			DrawSegment(center, start, end, i);
		}
	}

	private void DrawSegment(Vector2 center, float start, float end, int faceIndex)
	{
		var outline = SectorPoints(center, InnerRadius, OuterRadius, start, end);
		DrawPolygon(outline, Enumerable.Repeat(EmptyColor, outline.Length).ToArray());

		if (_max[faceIndex] > 0 && _current[faceIndex] > 0)
		{
			var fillRadius = Mathf.Lerp(
				InnerRadius,
				OuterRadius,
				_current[faceIndex] / (float)_max[faceIndex]);
			var fill = SectorPoints(center, InnerRadius, fillRadius, start, end);
			DrawPolygon(fill, Enumerable.Repeat(FillColor, fill.Length).ToArray());
		}

		DrawPolyline(outline, BorderColor, 1.5f, true);
	}

	private bool TryGetFaceIndex(Vector2 point, out int faceIndex)
	{
		faceIndex = -1;
		var offset = point - Size * 0.5f;
		var distance = offset.Length();
		if (distance < InnerRadius || distance > OuterRadius)
			return false;

		var angle = Mathf.Atan2(offset.Y, offset.X);
		if (angle < StartAngle)
			angle += Mathf.Tau;
		if (angle < StartAngle || angle > EndAngle)
			return false;

		var segmentSpan = (EndAngle - StartAngle) / Faces.Length;
		faceIndex = Mathf.FloorToInt((angle - StartAngle) / segmentSpan);
		if (faceIndex < 0 || faceIndex >= Faces.Length)
			return false;

		var segmentStart = StartAngle + segmentSpan * faceIndex + SegmentGap * 0.5f;
		var segmentEnd = StartAngle + segmentSpan * (faceIndex + 1) - SegmentGap * 0.5f;
		return angle >= segmentStart && angle <= segmentEnd;
	}

	private static Vector2[] SectorPoints(
		Vector2 center,
		float innerRadius,
		float outerRadius,
		float start,
		float end)
	{
		var points = new List<Vector2>((ArcPointCount + 1) * 2);
		for (var i = 0; i <= ArcPointCount; i++)
		{
			var angle = Mathf.Lerp(start, end, i / (float)ArcPointCount);
			points.Add(center + Vector2.FromAngle(angle) * outerRadius);
		}
		for (var i = ArcPointCount; i >= 0; i--)
		{
			var angle = Mathf.Lerp(start, end, i / (float)ArcPointCount);
			points.Add(center + Vector2.FromAngle(angle) * innerRadius);
		}
		return points.ToArray();
	}
}

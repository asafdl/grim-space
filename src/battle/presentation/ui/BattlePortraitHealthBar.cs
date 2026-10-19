using Godot;

namespace GrimSpace.Battle.Presentation.Ui;

public sealed partial class BattlePortraitHealthBar : Control
{
	private const float TrackRadius = 57f;
	private const float TrackWidth = 9f;
	private const float StartAngle = Mathf.Pi * 0.72f;
	private const float EndAngle = Mathf.Pi * 1.69f;
	private const int ArcPointCount = 48;

	private static readonly Color EmptyColor = new(0.16f, 0.035f, 0.055f, 0.96f);
	private static readonly Color FillColor = new(0.82f, 0.08f, 0.16f, 1f);
	private static readonly Color BorderColor = new(0.96f, 0.96f, 1f, 0.96f);

	private int _current;
	private int _max = 1;

	public BattlePortraitHealthBar()
	{
		CustomMinimumSize = new Vector2(124f, 124f);
		MouseFilter = MouseFilterEnum.Stop;
		TooltipText = BattleHudCopy.HullTooltip;
		Set(0, 1);
	}

	public void Set(int current, int max)
	{
		_max = System.Math.Max(0, max);
		_current = System.Math.Clamp(current, 0, _max);
		QueueRedraw();
	}

	public override bool _HasPoint(Vector2 point)
	{
		var offset = point - Size * 0.5f;
		var distance = offset.Length();
		if (distance < TrackRadius - TrackWidth * 0.5f
			|| distance > TrackRadius + TrackWidth * 0.5f)
			return false;

		var angle = Mathf.Atan2(offset.Y, offset.X);
		if (angle < StartAngle)
			angle += Mathf.Tau;
		return angle >= StartAngle && angle <= EndAngle;
	}

	public override string _GetTooltip(Vector2 atPosition) =>
		$"Hull {_current}/{_max}";

	public override void _Draw()
	{
		var center = Size * 0.5f;
		DrawArc(
			center,
			TrackRadius,
			StartAngle,
			EndAngle,
			ArcPointCount,
			EmptyColor,
			TrackWidth,
			true);

		if (_max > 0 && _current > 0)
		{
			var fillEnd = Mathf.Lerp(
				StartAngle,
				EndAngle,
				_current / (float)_max);
			DrawArc(
				center,
				TrackRadius,
				StartAngle,
				fillEnd,
				ArcPointCount,
				FillColor,
				TrackWidth,
				true);
		}

		DrawArc(
			center,
			TrackRadius - TrackWidth * 0.5f,
			StartAngle,
			EndAngle,
			ArcPointCount,
			BorderColor,
			1.5f,
			true);
		DrawArc(
			center,
			TrackRadius + TrackWidth * 0.5f,
			StartAngle,
			EndAngle,
			ArcPointCount,
			BorderColor,
			1.5f,
			true);

		DrawBorderCap(center, StartAngle);
		DrawBorderCap(center, EndAngle);
	}

	private void DrawBorderCap(Vector2 center, float angle)
	{
		var direction = Vector2.FromAngle(angle);
		DrawLine(
			center + direction * (TrackRadius - TrackWidth * 0.5f),
			center + direction * (TrackRadius + TrackWidth * 0.5f),
			BorderColor,
			1.5f,
			true);
	}
}

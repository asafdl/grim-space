using Godot;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Ui;

public sealed record MountFaceSelectorOption(
	ESpatialOrientation Face,
	Color Color,
	bool Enabled);

public sealed partial class MountFaceSelector : Control
{
	private const float ControlSize = 250f;
	private const float ShellRadius = 66f;
	private const float ChevronRadius = 98f;
	private const float ChevronDepth = 23f;
	private const float ChevronHalfWidth = 17f;
	private const float HoverOffset = 7f;
	private const float DeadZoneRadius = 31f;
	private const float HitRadius = 122f;

	private static readonly Color ShellCenter = new(0.025f, 0.045f, 0.065f, 0.08f);
	private static readonly Color ShellEdge = new(0.025f, 0.045f, 0.065f, 0.72f);
	private static readonly Color ShellBorder = new(0.78f, 0.88f, 0.96f, 0.98f);
	private static readonly Color DisabledChevron = new(0.3f, 0.33f, 0.37f, 0.48f);
	private static readonly Color Shadow = new(0f, 0f, 0f, 0.72f);
	private static readonly FaceSlot[] Slots =
	[
		new(ESpatialOrientation.Dorsal, -Mathf.Pi / 2f),
		new(ESpatialOrientation.Forward, -Mathf.Pi / 6f),
		new(ESpatialOrientation.Starboard, Mathf.Pi / 6f),
		new(ESpatialOrientation.Ventral, Mathf.Pi / 2f),
		new(ESpatialOrientation.Retro, Mathf.Pi * 5f / 6f),
		new(ESpatialOrientation.Port, -Mathf.Pi * 5f / 6f),
	];

	private readonly Dictionary<ESpatialOrientation, MountFaceSelectorOption> _options =
		Slots.ToDictionary(
			slot => slot.Face,
			slot => new MountFaceSelectorOption(slot.Face, DisabledChevron, false));
	private ESpatialOrientation? _pointedFace;

	public MountFaceSelector()
	{
		CustomMinimumSize = new Vector2(ControlSize, ControlSize);
		Size = CustomMinimumSize;
		Visible = false;
		MouseFilter = MouseFilterEnum.Stop;
		FocusMode = FocusModeEnum.None;
		MouseExited += () => SetPointedFace(null);
	}

	public event Action<ESpatialOrientation?>? FaceHovered;
	public event Action<ESpatialOrientation>? FaceSelected;

	public ESpatialOrientation? HoveredFace =>
		_pointedFace is { } face && _options[face].Enabled ? face : null;

	public void SetOptions(IEnumerable<MountFaceSelectorOption> options)
	{
		ArgumentNullException.ThrowIfNull(options);
		var supplied = options.ToList();
		if (supplied.Count != Slots.Length
			|| supplied.Select(option => option.Face).Distinct().Count() != Slots.Length
			|| supplied.Any(option => !Enum.IsDefined(option.Face)))
		{
			throw new ArgumentException(
				"Mount face selector options must contain each spatial orientation exactly once.",
				nameof(options));
		}

		foreach (var option in supplied)
			_options[option.Face] = option;

		SetPointedFace(FaceAt(GetLocalMousePosition()));
		QueueRedraw();
	}

	public void ShowAt(Vector2 center, Rect2 bounds)
	{
		MoveTo(center, bounds);
		Visible = true;
	}

	public void MoveTo(Vector2 center, Rect2 bounds)
	{
		var desired = center - Size * 0.5f;
		var maxX = System.Math.Max(bounds.Position.X, bounds.End.X - Size.X);
		var maxY = System.Math.Max(bounds.Position.Y, bounds.End.Y - Size.Y);
		Position = new Vector2(
			Mathf.Clamp(desired.X, bounds.Position.X, maxX),
			Mathf.Clamp(desired.Y, bounds.Position.Y, maxY));
		SetPointedFace(FaceAt(GetLocalMousePosition()));
	}

	public void Dismiss()
	{
		Visible = false;
		SetPointedFace(null);
	}

	public override bool _HasPoint(Vector2 point) =>
		(point - Size * 0.5f).LengthSquared() <= HitRadius * HitRadius;

	public override void _GuiInput(InputEvent @event)
	{
		switch (@event)
		{
			case InputEventMouseMotion motion:
				SetPointedFace(FaceAt(motion.Position));
				AcceptEvent();
				break;
			case InputEventMouseButton
				{
					Pressed: true,
					ButtonIndex: MouseButton.Left,
				}:
				if (HoveredFace is { } selected)
					FaceSelected?.Invoke(selected);
				AcceptEvent();
				break;
		}
	}

	public override void _Draw()
	{
		var center = Size * 0.5f;
		var shell = RegularPolygon(center, ShellRadius, 6, -Mathf.Pi / 6f);
		for (var i = 0; i < shell.Length; i++)
		{
			Vector2[] fadeTriangle = [center, shell[i], shell[(i + 1) % shell.Length]];
			DrawPolygon(fadeTriangle, [ShellCenter, ShellEdge, ShellEdge]);
		}
		Vector2[] outline = [.. shell, shell[0]];
		DrawPolyline(outline, Shadow, 8f, true);
		DrawPolyline(outline, ShellBorder, 3.5f, true);

		foreach (var slot in Slots)
			DrawChevron(center, slot);
	}

	private void DrawChevron(Vector2 center, FaceSlot slot)
	{
		var option = _options[slot.Face];
		var hovered = _pointedFace == slot.Face;
		var direction = Vector2.FromAngle(slot.Angle);
		var tangent = new Vector2(-direction.Y, direction.X);
		var radius = ChevronRadius + (hovered ? HoverOffset : 0f);
		var baseCenter = center + direction * (radius - ChevronDepth * 0.5f);
		var apex = center + direction * (radius + ChevronDepth * 0.5f);
		Vector2[] points =
		[
			baseCenter + tangent * ChevronHalfWidth,
			apex,
			baseCenter - tangent * ChevronHalfWidth,
		];
		var color = option.Enabled ? option.Color : DisabledChevron;
		if (hovered && option.Enabled)
			color = color.Lerp(Colors.White, 0.28f);
		if (option.Enabled)
		{
			var glow = new Color(color.R, color.G, color.B, hovered ? 0.58f : 0.32f);
			DrawPolyline(points, Shadow, hovered ? 13f : 10f, true);
			DrawPolyline(points, glow, hovered ? 11f : 8f, true);
		}
		DrawPolyline(
			points,
			color,
			option.Enabled ? hovered ? 7f : 5f : 3f,
			true);
	}

	private ESpatialOrientation? FaceAt(Vector2 point)
	{
		var offset = point - Size * 0.5f;
		var distanceSquared = offset.LengthSquared();
		if (distanceSquared < DeadZoneRadius * DeadZoneRadius
			|| distanceSquared > HitRadius * HitRadius)
			return null;

		var angle = Mathf.Atan2(offset.Y, offset.X);
		return Slots
			.MinBy(slot => AngularDistance(angle, slot.Angle))
			.Face;
	}

	private void SetPointedFace(ESpatialOrientation? face)
	{
		if (_pointedFace == face)
			return;

		_pointedFace = face;
		FaceHovered?.Invoke(HoveredFace);
		QueueRedraw();
	}

	private static float AngularDistance(float first, float second) =>
		System.Math.Abs(Mathf.Wrap(first - second, -Mathf.Pi, Mathf.Pi));

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

	private readonly record struct FaceSlot(
		ESpatialOrientation Face,
		float Angle);
}

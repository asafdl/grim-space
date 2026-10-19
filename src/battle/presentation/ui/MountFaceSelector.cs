using Godot;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Ui;

public sealed record MountFaceSelectorOption(
	ESpatialOrientation Face,
	Color Color,
	bool Enabled,
	Vector2 Position,
	float Rotation,
	MovementSelection.HeadingHandleKind Kind);

public sealed partial class MountFaceSelector : Control
{
	private IReadOnlyList<MountFaceSelectorOption> _options = [];
	private Vector2 _center;
	private ESpatialOrientation? _pointedFace;

	public MountFaceSelector()
	{
		Visible = false;
		MouseFilter = MouseFilterEnum.Stop;
		FocusMode = FocusModeEnum.None;
		MouseExited += () => SetPointedFace(null);
	}

	public event Action<ESpatialOrientation?>? FaceHovered;
	public event Action<ESpatialOrientation>? FaceSelected;

	public ESpatialOrientation? HoveredFace =>
		_pointedFace is { } face && OptionFor(face).Enabled ? face : null;

	public void ShowProjected(
		Vector2 center,
		IEnumerable<MountFaceSelectorOption> options)
	{
		ArgumentNullException.ThrowIfNull(options);
		var supplied = options.ToList();
		if (supplied.Count != Enum.GetValues<ESpatialOrientation>().Length
			|| supplied.Select(option => option.Face).Distinct().Count() != supplied.Count
			|| supplied.Any(option => !Enum.IsDefined(option.Face)))
		{
			throw new ArgumentException(
				"Mount face selector options must contain each spatial orientation exactly once.",
				nameof(options));
		}

		_center = center;
		_options = supplied;
		Visible = true;
		SetPointedFace(FaceAt(GetLocalMousePosition()));
		QueueRedraw();
	}

	public void Dismiss()
	{
		Visible = false;
		SetPointedFace(null);
	}

	public override bool _HasPoint(Vector2 point) =>
		FaceAt(point) is not null;

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
		ProjectedDirectionHandleRenderer.DrawShell(this, _center);
		foreach (var option in _options)
			DrawHandle(option);
	}

	private void DrawHandle(MountFaceSelectorOption option)
	{
		ProjectedDirectionHandleRenderer.Draw(
			this,
			option.Position,
			option.Rotation,
			option.Kind,
			option.Color,
			option.Enabled,
			_pointedFace == option.Face);
	}

	private ESpatialOrientation? FaceAt(Vector2 point)
	{
		MountFaceSelectorOption? closest = null;
		var closestDistanceSquared =
			ProjectedDirectionHandleRenderer.HitRadius
			* ProjectedDirectionHandleRenderer.HitRadius;
		foreach (var option in _options)
		{
			var distanceSquared = point.DistanceSquaredTo(option.Position);
			if (distanceSquared > closestDistanceSquared)
				continue;

			closest = option;
			closestDistanceSquared = distanceSquared;
		}

		return closest?.Face;
	}

	private MountFaceSelectorOption OptionFor(ESpatialOrientation face) =>
		_options.First(option => option.Face == face);

	private void SetPointedFace(ESpatialOrientation? face)
	{
		if (_pointedFace == face)
			return;

		_pointedFace = face;
		FaceHovered?.Invoke(HoveredFace);
		QueueRedraw();
	}

}

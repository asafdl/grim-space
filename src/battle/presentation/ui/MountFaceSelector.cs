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
	private Vector2 _buzzOffset;
	private Tween? _buzzTween;
	private readonly RandomNumberGenerator _buzzRng = new();

	public MountFaceSelector()
	{
		Visible = false;
		MouseFilter = MouseFilterEnum.Ignore;
		FocusMode = FocusModeEnum.None;
	}

	public event Action<ESpatialOrientation?>? FaceHovered;

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
		QueueRedraw();
	}

	public void Dismiss()
	{
		Visible = false;
		SetPointedFace(null);
		_buzzTween?.Kill();
		_buzzTween = null;
		_buzzOffset = Vector2.Zero;
	}

	public (ESpatialOrientation? Face, bool IsOutside) Pick(Vector2 point)
	{
		var result = MovementSelection.ResolveDirectionPick(
			point, _center, _options.Select(option => option.Position).ToArray());
		var option = result.HandleIndex is { } index ? _options[index] : null;
		return (option is { Enabled: true } ? option.Face : null, result.IsOutside);
	}

	public void SetHoveredFace(ESpatialOrientation? face) => SetPointedFace(face);

	public void NudgeAttention()
	{
		if (!Visible)
			return;

		_buzzTween?.Kill();
		_buzzTween = ProjectedDirectionHandleRenderer.StartAttention(this, _buzzRng, offset =>
		{
			_buzzOffset = offset;
			QueueRedraw();
		});
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
			option.Position + _buzzOffset,
			option.Rotation,
			option.Kind,
			option.Color,
			option.Enabled,
			_pointedFace == option.Face);
	}

	private void SetPointedFace(ESpatialOrientation? face)
	{
		if (_pointedFace == face)
			return;

		_pointedFace = face;
		FaceHovered?.Invoke(face);
		QueueRedraw();
	}

}

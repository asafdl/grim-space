using Godot;

namespace GrimSpace.Education;

public sealed partial class TutorialDialogHost : CanvasLayer
{
	private const int DialogWidth = 560;
	private const int DialogMargin = 24;

	private readonly TutorialDialog _dialog;

	public ITutorialDialog Dialog => _dialog;

	public TutorialDialogHost()
	{
		Layer = 25;
		Name = "TutorialDialogHost";
		_dialog = new TutorialDialog();
		AddChild(_dialog);
		ApplyMapLayout();
	}

	public void ApplyMapLayout() => ApplyBottomLeftLayout();

	public void ApplyBattleLayout() => ApplyBottomLeftLayout();

	private void ApplyBottomLeftLayout()
	{
		_dialog.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
		_dialog.AnchorLeft = 0f;
		_dialog.AnchorRight = 0f;
		_dialog.AnchorTop = 1f;
		_dialog.AnchorBottom = 1f;
		_dialog.OffsetLeft = DialogMargin;
		_dialog.OffsetTop = -DialogMargin;
		_dialog.OffsetRight = DialogMargin + DialogWidth;
		_dialog.OffsetBottom = -DialogMargin;
		_dialog.GrowHorizontal = Control.GrowDirection.End;
		_dialog.GrowVertical = Control.GrowDirection.Begin;
	}
}

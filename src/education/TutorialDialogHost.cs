using Godot;

namespace GrimSpace.Education;

public sealed partial class TutorialDialogHost : CanvasLayer
{
	private const int DialogWidth = 560;
	private const int DialogTop = 32;

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

	public void ApplyMapLayout() => ApplyLayout();

	public void ApplyBattleLayout() => ApplyLayout();

	private void ApplyLayout()
	{
		_dialog.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
		_dialog.AnchorLeft = 0.5f;
		_dialog.AnchorRight = 0.5f;
		_dialog.OffsetLeft = -DialogWidth * 0.5f;
		_dialog.OffsetTop = DialogTop;
		_dialog.OffsetRight = DialogWidth * 0.5f;
		_dialog.OffsetBottom = DialogTop;
	}
}

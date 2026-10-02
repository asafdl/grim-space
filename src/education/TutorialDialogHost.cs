using Godot;

namespace GrimSpace.Education;

public sealed partial class TutorialDialogHost : CanvasLayer
{
	private const int DialogWidth = 560;
	private const int DialogTop = 32;
	private const int BattleDialogTop = 84;

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

	public void ApplyMapLayout() => ApplyLayout(DialogTop);

	public void ApplyBattleLayout() => ApplyLayout(BattleDialogTop);

	private void ApplyLayout(int dialogTop)
	{
		_dialog.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
		_dialog.AnchorLeft = 0.5f;
		_dialog.AnchorRight = 0.5f;
		_dialog.OffsetLeft = -DialogWidth * 0.5f;
		_dialog.OffsetTop = dialogTop;
		_dialog.OffsetRight = DialogWidth * 0.5f;
		_dialog.OffsetBottom = dialogTop;
	}
}

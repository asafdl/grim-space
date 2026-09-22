using Godot;
using GrimSpace.Components;

namespace GrimSpace.Education;

public sealed partial class TutorialDialogHost : CanvasLayer
{
	private const int DialogWidth = 380;
	private const int MapDialogTop = 270;
	private const int BattleDialogTop = 120;

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

	public void ApplyMapLayout() => ApplyLayout(MapDialogTop);

	public void ApplyBattleLayout() => ApplyLayout(BattleDialogTop);

	private void ApplyLayout(int top)
	{
		_dialog.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
		_dialog.OffsetLeft = HudStyles.Margin;
		_dialog.OffsetTop = top;
		_dialog.OffsetRight = DialogWidth + HudStyles.Margin;
		_dialog.OffsetBottom = top;
	}
}

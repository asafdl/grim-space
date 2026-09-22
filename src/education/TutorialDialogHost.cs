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

	public void ApplyMapLayout()
	{
		_dialog.SetAnchorsPreset(Control.LayoutPreset.TopRight);
		_dialog.OffsetLeft = -DialogWidth - HudStyles.Margin;
		_dialog.OffsetTop = MapDialogTop;
		_dialog.OffsetRight = -HudStyles.Margin;
		_dialog.OffsetBottom = MapDialogTop;
	}

	public void ApplyBattleLayout()
	{
		_dialog.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
		_dialog.OffsetLeft = HudStyles.Margin;
		_dialog.OffsetTop = BattleDialogTop;
		_dialog.OffsetRight = DialogWidth + HudStyles.Margin;
		_dialog.OffsetBottom = BattleDialogTop;
	}
}

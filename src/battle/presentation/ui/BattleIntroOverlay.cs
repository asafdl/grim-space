using Godot;
using GrimSpace.Battle.Objectives;
using GrimSpace.Presentation.Ui.Hud;

namespace GrimSpace.Battle.Presentation.Ui;

public sealed partial class BattleIntroOverlay : CanvasLayer
{
	private Label _title = null!;
	private Label _subtitle = null!;

	public BattleIntroOverlay()
	{
		Layer = 18;
		Build();
		Visible = false;
	}

	public void SetObjective(EObjective objective)
	{
		_title.Text = BattleHudCopy.IntroTitle;
		_subtitle.Text = BattleHudCopy.ObjectiveLabel(objective);
	}

	private void Build()
	{
		var root = new Control
		{
			AnchorsPreset = (int)Control.LayoutPreset.TopWide,
			AnchorRight = 1f,
			OffsetBottom = 96f,
			GrowHorizontal = Control.GrowDirection.Both,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		AddChild(root);

		var center = new CenterContainer
		{
			AnchorsPreset = (int)Control.LayoutPreset.FullRect,
			AnchorRight = 1f,
			AnchorBottom = 1f,
			GrowHorizontal = Control.GrowDirection.Both,
			GrowVertical = Control.GrowDirection.Both,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		root.AddChild(center);

		var banner = HudWidgets.CreateTopBanner(BattleHudCopy.IntroTitle, "");
		_title = banner.Title;
		_subtitle = banner.Subtitle;
		center.AddChild(banner.Root);
	}
}

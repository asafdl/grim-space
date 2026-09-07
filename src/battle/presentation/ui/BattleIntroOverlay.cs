using Godot;
using GrimSpace.Battle.Objectives;
using GrimSpace.Presentation.Ui.Hud;

namespace GrimSpace.Battle.Presentation.Ui;

public sealed partial class BattleIntroOverlay : CanvasLayer
{
	private Control _root = null!;
	private Label _title = null!;
	private Label _objective = null!;

	public BattleIntroOverlay()
	{
		Layer = 18;
		Build();
		Visible = false;
	}

	public void ApplyTheme(Theme theme) => _root.Theme = theme;

	public void SetObjective(EObjective objective)
	{
		_title.Text = BattleHudCopy.IntroTitle;
		_objective.Text = BattleHudCopy.ObjectiveLabel(objective);
	}

	private void Build()
	{
		_root = new Control
		{
			AnchorsPreset = (int)Control.LayoutPreset.TopWide,
			AnchorRight = 1f,
			OffsetBottom = 96f,
			GrowHorizontal = Control.GrowDirection.Both,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		AddChild(_root);

		var center = new CenterContainer
		{
			AnchorsPreset = (int)Control.LayoutPreset.FullRect,
			AnchorRight = 1f,
			AnchorBottom = 1f,
			GrowHorizontal = Control.GrowDirection.Both,
			GrowVertical = Control.GrowDirection.Both,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		_root.AddChild(center);

		var panel = new PanelContainer
		{
			CustomMinimumSize = new Vector2(520, 0),
		};
		panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
		{
			BgColor = new Color(0.06f, 0.08f, 0.12f, 0.92f),
			BorderColor = new Color(0.9f, 0.25f, 0.35f, 0.95f),
			BorderWidthLeft = 2,
			BorderWidthTop = 2,
			BorderWidthRight = 2,
			BorderWidthBottom = 2,
			CornerRadiusTopLeft = 6,
			CornerRadiusTopRight = 6,
			CornerRadiusBottomRight = 6,
			CornerRadiusBottomLeft = 6,
			ContentMarginLeft = 28,
			ContentMarginRight = 28,
			ContentMarginTop = 14,
			ContentMarginBottom = 14,
		});
		center.AddChild(panel);

		var content = new VBoxContainer
		{
			Alignment = BoxContainer.AlignmentMode.Center,
		};
		content.AddThemeConstantOverride("separation", 4);
		panel.AddChild(content);

		_title = new Label
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			ThemeTypeVariation = "BattleBannerTitle",
		};
		content.AddChild(_title);

		_objective = new Label
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			ThemeTypeVariation = "OverlayBody",
		};
		content.AddChild(_objective);
	}
}

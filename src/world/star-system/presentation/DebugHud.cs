using Godot;
using GrimSpace.Presentation.Ui.Hud;

namespace GrimSpace.World.StarSystem.Presentation;

public partial class DebugHud : MarginContainer
{
	public Label SystemLabel { get; private set; } = null!;
	public Label TickLabel { get; private set; } = null!;
	public Button PauseButton { get; private set; } = null!;
	public Button StepButton { get; private set; } = null!;
	public Button SpeedButton { get; private set; } = null!;
	public Button RebuildButton { get; private set; } = null!;

	public override void _Ready()
	{
		HudThemes.Apply(this, HudThemeFamily.Debug);
		ConfigureChrome();
		Build();
	}

	private void ConfigureChrome()
	{
		SetAnchorsPreset(LayoutPreset.TopLeft);
		MouseFilter = MouseFilterEnum.Ignore;

		AddThemeConstantOverride("margin_left", HudStyles.Margin);
		AddThemeConstantOverride("margin_top", HudStyles.Margin);
	}

	private void Build()
	{
		var shell = HudWidgets.CreateHudPanel("Debug", HudThemeFamily.Debug);
		AddChild(shell.Root);

		SystemLabel = new Label
		{
			Text = "Supply · seed 0 · copper",
			MouseFilter = MouseFilterEnum.Ignore,
			ThemeTypeVariation = HudStyles.EntryBodyVariation(HudThemeFamily.Debug),
		};
		shell.Body.AddChild(SystemLabel);

		TickLabel = new Label
		{
			Text = "Tick 0",
			MouseFilter = MouseFilterEnum.Ignore,
			ThemeTypeVariation = HudStyles.EntryBodyVariation(HudThemeFamily.Debug),
		};
		shell.Body.AddChild(TickLabel);

		var buttons = new HBoxContainer
		{
			MouseFilter = MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		buttons.AddThemeConstantOverride("separation", HudStyles.HalfMargin);
		shell.Body.AddChild(buttons);

		PauseButton = CreateButton("Pause");
		StepButton = CreateButton("Step");
		SpeedButton = CreateButton("Speed 1x");
		RebuildButton = CreateButton("Rebuild");
		buttons.AddChild(PauseButton);
		buttons.AddChild(StepButton);
		buttons.AddChild(SpeedButton);
		buttons.AddChild(RebuildButton);
	}

	private static Button CreateButton(string text) =>
		new()
		{
			Text = text,
			ThemeTypeVariation = "DebugButton",
		};
}

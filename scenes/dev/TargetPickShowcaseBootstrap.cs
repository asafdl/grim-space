using Godot;
using GrimSpace.Application;
using GrimSpace.Battle.Dev;

namespace GrimSpace.Scenes.Dev;

/// <summary>F6 entry: enables showcase mode and hosts <c>battle.tscn</c>.</summary>
public partial class TargetPickShowcaseBootstrap : Node
{
	public override void _EnterTree() => TargetPickShowcase.Enabled = true;

	public override void _ExitTree() => TargetPickShowcase.Enabled = false;

	public override void _Ready()
	{
		if (Session.Instance.Run is null)
			Session.Instance.StartNewRun();
		Session.Instance.Run!.ActiveBattle = null;

		AddChild(GD.Load<PackedScene>("res://scenes/battle.tscn").Instantiate());
		AddHint();
	}

	private void AddHint()
	{
		var layer = new CanvasLayer { Layer = 40 };
		AddChild(layer);
		var label = new Label
		{
			Text =
				"Target pick showcase\n"
				+ "Protection Drone — exact 1/1, auto-queues on pick\n"
				+ "Mark Targets — 1–3 enemies; when ready the border trail runs — click skill again",
			Position = new Vector2(20, 20),
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		label.AddThemeFontSizeOverride("font_size", 16);
		label.AddThemeColorOverride("font_color", new Color(0.92f, 0.94f, 0.98f));
		label.AddThemeColorOverride("font_outline_color", new Color(0.02f, 0.04f, 0.08f));
		label.AddThemeConstantOverride("outline_size", 4);
		layer.AddChild(label);
	}
}

using Godot;
using GrimSpace.Ui.Portraits;

namespace GrimSpace.Presentation.Dev;

public sealed partial class RobotPortraitPreview : Control
{
	public override void _Ready()
	{
		AddChild(new ColorRect
		{
			AnchorRight = 1f, AnchorBottom = 1f, Color = new("#111819"),
			MouseFilter = MouseFilterEnum.Ignore,
		});
		var scroll = new ScrollContainer { AnchorRight = 1f, AnchorBottom = 1f };
		AddChild(scroll);
		var margin = new MarginContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		foreach (var side in new[] { "left", "right", "top", "bottom" })
			margin.AddThemeConstantOverride($"margin_{side}", 24);
		scroll.AddChild(margin);
		var page = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		page.AddThemeConstantOverride("separation", 14);
		margin.AddChild(page);
		page.AddChild(Label("ROBOT AVATAR GENERATOR", 26));
		page.AddChild(Label("SVG parts / independent eyes and mouths / muted metals / bright LED faces", 15));

		var generated = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		page.AddChild(generated);
		var hero = new RobotPortrait { CustomMinimumSize = new Vector2(310, 310) };
		generated.AddChild(hero);
		var actions = new VBoxContainer { CustomMinimumSize = new Vector2(300, 0) };
		generated.AddChild(actions);
		var randomize = new Button { Text = "Randomize", CustomMinimumSize = new Vector2(220, 48) };
		actions.AddChild(randomize);
		var summary = Label("", 15);
		actions.AddChild(summary);
		var samples = new List<RobotPortrait>();
		void Randomize()
		{
			var seed = Random.Shared.Next();
			var avatar = RobotPortraitGenerator.Generate(seed);
			hero.Parts = avatar.Parts;
			hero.Palette = RobotPortraitPalette.FromColors(avatar.Colors);
			hero.Body = avatar.Body;
			summary.Text = $"Seed: {seed}\n{avatar.Parts.Eyes} eyes / {avatar.Parts.Mouth} mouth\n{avatar.Parts.Screen} screen\nCracks: {avatar.Parts.Cracks} / Tape: {avatar.Parts.Tape}\nRust: {avatar.Parts.Rust}";
			foreach (var sample in samples)
			{
				var next = RobotPortraitGenerator.Generate(Random.Shared.Next());
				sample.Parts = next.Parts;
				sample.Palette = RobotPortraitPalette.FromColors(next.Colors);
				sample.Body = next.Body;
			}
		}
		randomize.Pressed += Randomize;
		var gallery = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		page.AddChild(gallery);
		for (var i = 0; i < 6; i++)
		{
			var sample = new RobotPortrait
			{
				CustomMinimumSize = new Vector2(180, 180),
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
			};
			samples.Add(sample);
			gallery.AddChild(sample);
		}
		Randomize();
		page.AddChild(Label("BODY PROPORTIONS", 19));
		var bodies = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		page.AddChild(bodies);
		var bodyParts = new RobotPortraitParts(
			Eyes: RobotEyes.Friendly, Mouth: RobotMouth.Friendly,
			Cracks: RobotCracks.Hairline, Tape: RobotTape.Shoulder, Rust: RobotRust.Seams,
			CrackPlacement: new(RobotWearLocation.Screen, 160, 98, 0, 1),
			TapePlacement: new(RobotWearLocation.RightShoulder, 224, 288, 0, .65f));
		foreach (var (name, body) in new (string, RobotPortraitBody)[]
		{
			("Fat", new(RobotPortraitBody.WideWidth, 1f)),
			("Thin", new(RobotPortraitBody.ThinWidth, 1f)),
			("Short", new(1f, RobotPortraitBody.ShortHeight)),
			("Tall", new(1f, RobotPortraitBody.TallHeight)),
		})
		{
			var tile = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
			bodies.AddChild(tile);
			var row = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
			tile.AddChild(row);
			row.AddChild(new RobotPortrait { Body = body, Parts = bodyParts, CustomMinimumSize = new Vector2(210, 210) });
			tile.AddChild(new Label { Text = name, HorizontalAlignment = HorizontalAlignment.Center });
		}
	}

	private static Label Label(string text, int size)
	{
		var label = new Label { Text = text };
		label.AddThemeFontSizeOverride("font_size", size);
		label.AddThemeColorOverride("font_color", new Color("#cbd0bb"));
		return label;
	}
}

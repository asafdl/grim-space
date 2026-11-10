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
			summary.Text = $"Seed: {seed}\n{avatar.Parts.Eyes} eyes / {avatar.Parts.Mouth} mouth\n{avatar.Parts.Screen} screen\nCracks: {avatar.Parts.Cracks} / Tape: {avatar.Parts.Tape}\nRust: {avatar.Parts.Rust}";
			foreach (var sample in samples)
			{
				var next = RobotPortraitGenerator.Generate(Random.Shared.Next());
				sample.Parts = next.Parts;
				sample.Palette = RobotPortraitPalette.FromColors(next.Colors);
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
		page.AddChild(Label("FLAVOR OVERLAYS / Cracks, Tape, Rust, Stacked", 19));
		var effects = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		page.AddChild(effects);
		effects.AddChild(Tile("Chest crack", new(Cracks: RobotCracks.Branching,
			CrackPlacement: new(RobotWearLocation.Chest, 160, 279, 12, .9f)), RobotPortraitLayer.All, 210));
		effects.AddChild(Tile("Right shoulder tape", new(Tape: RobotTape.Crown,
			TapePlacement: new(RobotWearLocation.RightShoulder, 224, 288, -15, .65f)), RobotPortraitLayer.All, 210));
		effects.AddChild(Tile("Rust", new(Rust: RobotRust.Drips), RobotPortraitLayer.All, 210));
		effects.AddChild(Tile("Stacked", new(Cracks: RobotCracks.Hairline, Tape: RobotTape.Shoulder, Rust: RobotRust.Seams),
			RobotPortraitLayer.All, 210));
		page.AddChild(Label("NEW EXPRESSIONS / Smug, Hateful, Strange", 19));
		var expressions = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		page.AddChild(expressions);
		var newEyes = new[] { RobotEyes.Smug, RobotEyes.Hateful, RobotEyes.Strange };
		var newMouths = new[] { RobotMouth.Smug, RobotMouth.Hateful, RobotMouth.Strange };
		for (var i = 0; i < newEyes.Length; i++)
			expressions.AddChild(Tile(newEyes[i].ToString(),
				new(Eyes: newEyes[i], Mouth: newMouths[i]), RobotPortraitLayer.All, 230));
		page.AddChild(Label("PART LIBRARY", 19));

		var assembled = new GridContainer { Columns = 3 };
		page.AddChild(assembled);
		var eyes = Enum.GetValues<RobotEyes>();
		var mouths = Enum.GetValues<RobotMouth>();
		var screens = Enum.GetValues<RobotScreen>();
		var ears = Enum.GetValues<RobotEars>();
		var antennas = Enum.GetValues<RobotAntenna>();
		var shoulders = Enum.GetValues<RobotShoulders>();
		for (var i = 0; i < eyes.Length; i++)
		{
			var mouth = mouths[(i + 1) % mouths.Length];
			var parts = new RobotPortraitParts(eyes[i], mouth, screens[i % screens.Length], ears[i % ears.Length],
				antennas[i % antennas.Length], shoulders[i % shoulders.Length]);
			assembled.AddChild(Tile($"{eyes[i]} / {mouth}", parts, RobotPortraitLayer.All, 170));
		}

		var tabs = new TabContainer { CustomMinimumSize = new Vector2(0, 460), SizeFlagsHorizontal = SizeFlags.ExpandFill };
		page.AddChild(tabs);
		AddPartsTab(tabs, "Eyes", eyes, RobotPortraitLayer.Eyes, (p, v) => p with { Eyes = v });
		AddPartsTab(tabs, "Mouths", mouths, RobotPortraitLayer.Mouth, (p, v) => p with { Mouth = v });
		var combinations = new ScrollContainer { Name = "Eye + Mouth" };
		tabs.AddChild(combinations);
		var combinationGrid = new GridContainer { Columns = 6, SizeFlagsHorizontal = SizeFlags.ExpandFill };
		combinations.AddChild(combinationGrid);
		foreach (var eye in eyes)
			foreach (var mouth in mouths)
				combinationGrid.AddChild(Tile($"{eye} / {mouth}",
					new(Eyes: eye, Mouth: mouth), RobotPortraitLayer.All, 140));
		AddPartsTab(tabs, "Screens", screens, RobotPortraitLayer.Screen, (p, v) => p with { Screen = v });
		AddPartsTab(tabs, "Ears", ears, RobotPortraitLayer.Ears, (p, v) => p with { Ears = v });
		AddPartsTab(tabs, "Antennas", antennas, RobotPortraitLayer.Antenna, (p, v) => p with { Antenna = v });
		AddPartsTab(tabs, "Pauldrons", shoulders, RobotPortraitLayer.Shoulders, (p, v) => p with { Shoulders = v });
		AddPartsTab(tabs, "Chest", new[] { 0 }, RobotPortraitLayer.Chest, (p, _) => p);
		AddPartsTab(tabs, "Cracks", Enum.GetValues<RobotCracks>(), RobotPortraitLayer.Cracks, (p, v) => p with { Cracks = v });
		AddPartsTab(tabs, "Tape", Enum.GetValues<RobotTape>(), RobotPortraitLayer.Tape, (p, v) => p with { Tape = v });
		AddPartsTab(tabs, "Rust", Enum.GetValues<RobotRust>(), RobotPortraitLayer.Rust, (p, v) => p with { Rust = v });

		page.AddChild(Label("Randomize selects parts, colors and stackable overlays. Blink timing and pilot integration are later steps.", 14));
	}

	private static void AddPartsTab<T>(TabContainer tabs, string name, T[] values,
		RobotPortraitLayer layer, Func<RobotPortraitParts, T, RobotPortraitParts> select)
	{
		var scroll = new ScrollContainer { Name = name };
		tabs.AddChild(scroll);
		var grid = new GridContainer { Columns = 3, SizeFlagsHorizontal = SizeFlags.ExpandFill };
		scroll.AddChild(grid);
		foreach (var value in values)
			grid.AddChild(Tile(value?.ToString() ?? name, select(new(), value), layer, 165));
	}

	private static Control Tile(string name, RobotPortraitParts parts, RobotPortraitLayer layer, int size)
	{
		var tile = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		var row = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		tile.AddChild(row);
		row.AddChild(new RobotPortrait
		{
			Parts = parts, Layer = layer, CustomMinimumSize = new Vector2(size, size),
		});
		if (layer != RobotPortraitLayer.All)
			row.AddChild(new RobotPortrait { Parts = parts, CustomMinimumSize = new Vector2(size, size) });
		tile.AddChild(new Label { Text = name, HorizontalAlignment = HorizontalAlignment.Center });
		return tile;
	}

	private static Label Label(string text, int size)
	{
		var label = new Label { Text = text };
		label.AddThemeFontSizeOverride("font_size", size);
		label.AddThemeColorOverride("font_color", new Color("#cbd0bb"));
		return label;
	}
}

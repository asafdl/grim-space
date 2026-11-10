using System.Xml.Linq;
using GrimSpace.Ui.Portraits;

namespace GrimSpace.Tests.Ui;

public sealed class RobotPortraitAssetTests
{
	[Fact]
	public void EverySelectablePartHasAValidSharedCanvasSvg()
	{
		var assets = new List<string> { "chest/standard", "backdrops/soot" };
		assets.AddRange(Enum.GetValues<RobotEyes>().Select(v => $"faces/{v.ToString().ToLowerInvariant()}_eyes"));
		assets.AddRange(Enum.GetValues<RobotMouth>().Select(v => $"faces/{v.ToString().ToLowerInvariant()}_mouth"));
		foreach (var screen in Enum.GetValues<RobotScreen>())
		{
			assets.Add($"screens/{screen.ToString().ToLowerInvariant()}_bezel");
			assets.Add($"screens/{screen.ToString().ToLowerInvariant()}_glass");
		}
		assets.AddRange(Enum.GetValues<RobotEars>().Select(v => $"ears/{v.ToString().ToLowerInvariant()}"));
		assets.AddRange(Enum.GetValues<RobotShoulders>().Select(v => $"shoulders/{v.ToString().ToLowerInvariant()}"));
		assets.AddRange(Enum.GetValues<RobotAntenna>()
			.Where(v => v != RobotAntenna.None).Select(v => $"antennas/{v.ToString().ToLowerInvariant()}"));

		assets.AddRange(Enum.GetValues<RobotCracks>().Where(v => v != RobotCracks.None)
			.Select(v => $"cracks/{v.ToString().ToLowerInvariant()}"));
		assets.AddRange(Enum.GetValues<RobotTape>().Where(v => v != RobotTape.None)
			.Select(v => $"tape/{v.ToString().ToLowerInvariant()}"));
		assets.AddRange(Enum.GetValues<RobotRust>().Where(v => v != RobotRust.None)
			.Select(v => $"rust/{v.ToString().ToLowerInvariant()}"));
		Assert.Equal(51, assets.Count);
		var root = AssetDirectory();
		foreach (var asset in assets)
		{
			var svg = XDocument.Load(Path.Combine(root, $"{asset}.svg")).Root!;
			Assert.Equal(XName.Get("svg", "http://www.w3.org/2000/svg"), svg.Name);
			Assert.Equal("320", svg.Attribute("width")?.Value);
			Assert.Equal("320", svg.Attribute("height")?.Value);
			Assert.Equal("0 0 320 320", svg.Attribute("viewBox")?.Value);
			Assert.NotEmpty(svg.Elements());
		}
		Assert.Equal(assets.Count, Directory.GetFiles(root, "*.svg", SearchOption.AllDirectories).Length);
	}

	public static IEnumerable<object[]> EyeMouthPairs() =>
		from eyes in Enum.GetValues<RobotEyes>()
		from mouth in Enum.GetValues<RobotMouth>()
		select new object[] { eyes, mouth };

	[Theory]
	[MemberData(nameof(EyeMouthPairs))]
	public void EyesAndMouthCanBeSelectedIndependently(RobotEyes eyes, RobotMouth mouth)
	{
		var parts = new RobotPortraitParts(Eyes: eyes, Mouth: mouth);
		Assert.Equal(eyes, parts.Eyes);
		Assert.Equal(mouth, parts.Mouth);
		var changedEyes = parts with { Eyes = RobotEyes.Friendly };
		Assert.Equal(mouth, changedEyes.Mouth);
		var changedMouth = parts with { Mouth = RobotMouth.Angry };
		Assert.Equal(eyes, changedMouth.Eyes);
		var root = AssetDirectory();
		Assert.True(File.Exists(Path.Combine(root, $"faces/{parts.Eyes.ToString().ToLowerInvariant()}_eyes.svg")));
		Assert.True(File.Exists(Path.Combine(root, $"faces/{parts.Mouth.ToString().ToLowerInvariant()}_mouth.svg")));
	}

	[Fact]
	public void MonitorAndFaceAssetsShareTheSameSeparatedSquareCellGrid()
	{
		var root = AssetDirectory();
		XNamespace ns = "http://www.w3.org/2000/svg";
		var assets = Directory.GetFiles(Path.Combine(root, "faces"), "*.svg")
			.Concat(Directory.GetFiles(Path.Combine(root, "screens"), "*_glass.svg")).ToArray();
		Assert.Equal(24, assets.Length);
		foreach (var asset in assets)
		{
			var svg = XDocument.Load(asset);
			Assert.Empty(svg.Descendants(ns + "pattern"));
			var cells = svg.Descendants(ns + "rect").ToArray();
			Assert.NotEmpty(cells);
			foreach (var cell in cells)
			{
				Assert.Equal("6", cell.Attribute("width")?.Value);
				Assert.Equal("6", cell.Attribute("height")?.Value);
				Assert.Equal(0, int.Parse(cell.Attribute("x")!.Value) % 8);
				Assert.Equal(0, int.Parse(cell.Attribute("y")!.Value) % 8);
			}
			if (asset.EndsWith("_glass.svg", StringComparison.Ordinal))
			{
				Assert.Contains(svg.Descendants(), element => element.Attribute("fill")?.Value == "#404040");
				Assert.True(cells.Length > 100, $"Missing monitor cell field in '{asset}'.");
			}
			else
			{
				Assert.Empty(svg.Descendants(ns + "circle"));
				Assert.DoesNotContain(svg.Descendants(), element => element.Attribute("stroke") is not null);
			}
		}
	}

	private static string AssetDirectory()
	{
		for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
			directory is not null; directory = directory.Parent)
		{
			if (File.Exists(Path.Combine(directory.FullName, "project.godot")))
				return Path.Combine(directory.FullName, "assets", "ui", "robot_portraits");
		}
		throw new DirectoryNotFoundException("Could not locate robot portrait assets from the test output directory.");
	}
}

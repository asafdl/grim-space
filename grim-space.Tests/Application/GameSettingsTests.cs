using GrimSpace.Application;

namespace GrimSpace.Tests.Application;

public sealed class GameSettingsTests
{
	[Fact]
	public void SupportedResolutionsIncludeCommonDesktopSizes()
	{
		Assert.Contains((3840, 2160), ResolutionPairs(GameSettings.SupportedResolutions));
		Assert.Contains((2560, 1440), ResolutionPairs(GameSettings.SupportedResolutions));
		Assert.Contains((1920, 1080), ResolutionPairs(GameSettings.SupportedResolutions));
		Assert.Contains((1280, 720), ResolutionPairs(GameSettings.SupportedResolutions));
	}

	[Theory]
	[InlineData(5120, 2880, 2f, 2560, 1440)]
	[InlineData(2560, 1440, 1f, 2560, 1440)]
	[InlineData(1920, 1200, 1f, 1920, 1080)]
	[InlineData(1024, 576, 1f, 1280, 720)]
	public void FitResolutionToScreenSelectsHighestPresetThatFits(
		int screenWidth,
		int screenHeight,
		float screenScale,
		int expectedWidth,
		int expectedHeight)
	{
		var resolution = GameSettings.FitResolutionToScreen(
			new Godot.Vector2I(screenWidth, screenHeight),
			screenScale);

		Assert.Equal(expectedWidth, resolution.X);
		Assert.Equal(expectedHeight, resolution.Y);
	}

	[Fact]
	public void InvalidResolutionFallsBackToDesignResolution()
	{
		var normalized = GameSettings.NormalizeResolution(1234, 567);

		Assert.Equal(GameSettings.DesignCanvasSize.X, normalized.X);
		Assert.Equal(GameSettings.DesignCanvasSize.Y, normalized.Y);
	}

	private static IEnumerable<(int Width, int Height)> ResolutionPairs(
		IEnumerable<Godot.Vector2I> resolutions) =>
		resolutions.Select(resolution => (resolution.X, resolution.Y));
}

using Godot;
using GrimSpace.Application;

namespace GrimSpace.Tests.Application;

public sealed class GameSettingsTests
{
	[Fact]
	public void SupportedResolutionsIncludeCommonDesktopSizes()
	{
		Assert.Contains(new Vector2I(3840, 2160), GameSettings.SupportedResolutions);
		Assert.Contains(new Vector2I(2560, 1440), GameSettings.SupportedResolutions);
		Assert.Contains(new Vector2I(1920, 1080), GameSettings.SupportedResolutions);
		Assert.Contains(new Vector2I(1280, 720), GameSettings.SupportedResolutions);
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
			new Vector2I(screenWidth, screenHeight),
			screenScale);

		Assert.Equal(new Vector2I(expectedWidth, expectedHeight), resolution);
	}

	[Fact]
	public void InvalidResolutionFallsBackToDesignResolution()
	{
		Assert.Equal(GameSettings.DesignCanvasSize, GameSettings.NormalizeResolution(1234, 567));
	}
}

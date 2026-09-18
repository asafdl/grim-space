using Godot;
using GrimSpace.Application;

namespace GrimSpace.Tests.Application;

public sealed class GameSettingsTests
{
	[Fact]
	public void WriteVideoConfigDoesNotRequireLegacyRenderScale()
	{
		var config = new ConfigFile();
		var video = new GameSettings.VideoConfig(
			GameSettings.DisplayMode.Windowed,
			new Vector2I(1920, 1080));

		GameSettings.WriteVideoConfig(config, video);

		Assert.Equal("windowed", config.GetValue("video", "mode").AsString());
		Assert.Equal(1920, config.GetValue("video", "width").AsInt32());
		Assert.Equal(1080, config.GetValue("video", "height").AsInt32());
		Assert.False(config.HasSectionKey("video", "render_scale"));
	}

	[Fact]
	public void WriteVideoConfigRemovesLegacyRenderScale()
	{
		var config = new ConfigFile();
		config.SetValue("video", "render_scale", 0.5f);
		var video = new GameSettings.VideoConfig(
			GameSettings.DisplayMode.BorderlessFullscreen,
			new Vector2I(2560, 1440));

		GameSettings.WriteVideoConfig(config, video);

		Assert.False(config.HasSectionKey("video", "render_scale"));
		Assert.Equal("borderless_fullscreen", config.GetValue("video", "mode").AsString());
	}
}

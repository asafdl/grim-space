using GrimSpace.Components;

namespace GrimSpace.Tests.Ui;

public sealed class HudTooltipIconTests
{
	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("  ")]
	public void IconWithoutDescriptionHasNoTooltip(string? description)
	{
		var icon = new HudTooltipIconDef("unused", default, description);

		Assert.Null(icon.Tooltip("Test"));
	}

	[Fact]
	public void IconWithDescriptionHasTooltip()
	{
		var icon = new HudTooltipIconDef("unused", default, "description");

		Assert.NotNull(icon.Tooltip("Test"));
	}

	[Theory]
	[InlineData(null)]
	[InlineData("description")]
	public void OverrideTakesPrecedenceEvenWithoutDescription(string? description)
	{
		var icon = new HudTooltipIconDef("unused", default, description);
		var overrideContent = HudTooltipContent.Plain("override");
		var overrides = new Dictionary<string, HudTooltipContent> { ["Test"] = overrideContent };

		Assert.Same(overrideContent, icon.Tooltip("Test", overrides));
		Assert.Equal(description is not null, icon.Tooltip("Other", overrides) is not null);
	}
}

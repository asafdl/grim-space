using GrimSpace.Ui.Components;

namespace GrimSpace.Tests.Components;

[BattleTestSuite]
public sealed class UiSizeTests
{
	[Fact]
	public void No_override_preserves_default_size()
	{
		var defaults = new UiSize(145f, 44f);

		Assert.Equal(defaults, defaults.WithOverride(null));
		Assert.Equal(defaults, defaults.WithOverride(new UiSizeOverride()));
	}

	[Fact]
	public void Width_override_preserves_default_height()
	{
		Assert.Equal(
			new UiSize(280f, 44f),
			new UiSize(145f, 44f).WithOverride(new UiSizeOverride(Width: 280f)));
	}

	[Fact]
	public void Height_override_preserves_default_width()
	{
		Assert.Equal(
			new UiSize(145f, 60f),
			new UiSize(145f, 44f).WithOverride(new UiSizeOverride(Height: 60f)));
	}

	[Fact]
	public void Override_can_replace_both_dimensions_or_release_one_to_content()
	{
		Assert.Equal(
			new UiSize(0f, 60f),
			new UiSize(145f, 44f).WithOverride(new UiSizeOverride(0f, 60f)));
	}

	[Fact]
	public void Changing_defaults_preserves_overridden_axis_and_updates_unspecified_axis()
	{
		var sizeOverride = new UiSizeOverride(Width: 280f);

		Assert.Equal(new UiSize(280f, 44f), new UiSize(145f, 44f).WithOverride(sizeOverride));
		Assert.Equal(new UiSize(280f, 54f), new UiSize(159f, 54f).WithOverride(sizeOverride));
	}

	[Fact]
	public void Clearing_override_restores_current_defaults()
	{
		var defaults = new UiSize(159f, 54f);
		UiSizeOverride? sizeOverride = new UiSizeOverride(Width: 280f);
		Assert.Equal(new UiSize(280f, 54f), defaults.WithOverride(sizeOverride));

		sizeOverride = null;

		Assert.Equal(defaults, defaults.WithOverride(sizeOverride));
	}

	[Theory]
	[InlineData(-1f)]
	[InlineData(float.NaN)]
	[InlineData(float.PositiveInfinity)]
	[InlineData(float.NegativeInfinity)]
	public void Invalid_dimensions_are_rejected(float dimension)
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => new UiSize(dimension, 0f));
		Assert.Throws<ArgumentOutOfRangeException>(() => new UiSize(0f, dimension));
		Assert.Throws<ArgumentOutOfRangeException>(() => new UiSizeOverride(Width: dimension));
		Assert.Throws<ArgumentOutOfRangeException>(() => new UiSizeOverride(Height: dimension));
	}
}

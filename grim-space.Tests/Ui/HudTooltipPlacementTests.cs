using Godot;
using GrimSpace.Components;

namespace GrimSpace.Tests.Ui;

public sealed class HudTooltipPlacementTests
{
	[Theory]
	[InlineData(true, 420, 280)]
	[InlineData(false, 300, 220)]
	public void PreservesPopupPositionWhenPinning(bool embedded, float popupX, float popupY)
	{
		var viewportTransform = new Transform2D(
			new Vector2(0.5f, 0),
			new Vector2(0, 0.5f),
			new Vector2(10, 20));

		var position = HudTooltipPin.PinnedPosition(
			new Vector2(popupX, popupY),
			embedded,
			new Vector2(80, 60),
			viewportTransform);

		Assert.Equal(new Vector2(420, 280), position);
	}

	[Theory]
	[InlineData(400, 300, 200, 150, 604, 300)]
	[InlineData(800, 300, 200, 150, 616, 300)]
	[InlineData(0, 300, 1000, 150, 0, 454)]
	[InlineData(0, 650, 1000, 150, 0, 566)]
	public void PlacesChildBesideParentWithoutOverlap(
		float x, float y, float width, float height, float expectedX, float expectedY)
	{
		var position = HudTooltipPin.ChildPosition(
			new Rect2(x, y, width, height),
			new Vector2(180, 80),
			new Rect2(0, 0, 1000, 800));

		Assert.Equal(new Vector2(expectedX, expectedY), position);
	}

	[Fact]
	public void KeepsChildOnScreenWhenNoNonOverlappingPositionFits()
	{
		var position = HudTooltipPin.ChildPosition(
			new Rect2(40, 0, 20, 100),
			new Vector2(70, 80),
			new Rect2(0, 0, 100, 100));

		Assert.InRange(position.X, 0, 30);
		Assert.InRange(position.Y, 0, 20);
	}
}

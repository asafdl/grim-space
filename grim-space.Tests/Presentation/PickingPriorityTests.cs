using GrimSpace.Battle.Presentation.Picking;

namespace GrimSpace.Tests.Presentation;

public sealed class PickingPriorityTests
{
	[Fact]
	public void GridPickPrefersNearestDepthOverSmallerRayOffset()
	{
		Assert.True(GridPick.IsBetter(
			depth: 10f,
			offset: 0.9f,
			bestDepth: 20f,
			bestOffset: 0.1f));
	}

	[Fact]
	public void GridPickUsesRayOffsetAtEqualDepth()
	{
		Assert.True(GridPick.IsBetter(
			depth: 10f,
			offset: 0.1f,
			bestDepth: 10f,
			bestOffset: 0.9f));
	}

	[Fact]
	public void UnitPickPrefersNearerUnitWithinCursorRadius()
	{
		Assert.True(UnitPick.IsBetter(
			screenDist: 30f,
			cameraDist: 10f,
			bestScreenDist: 0f,
			bestCameraDist: 20f));
	}
}

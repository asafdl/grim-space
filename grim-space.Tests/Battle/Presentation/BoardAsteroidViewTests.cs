using Godot;
using GrimSpace.Battle.Presentation.Graphics;

namespace grim_space.Tests.Battle.Presentation;

public sealed class BoardAsteroidViewTests
{
	private static readonly Aabb Bounds = new(Vector3.Zero, Vector3.One * 2f);

	[Fact]
	public void SegmentThroughBoundsIntersects()
	{
		Assert.True(BoardAsteroidView.SegmentIntersectsBounds(
			new Vector3(-2f, 1f, 1f),
			new Vector3(4f, 1f, 1f),
			Bounds));
	}

	[Fact]
	public void SegmentBeforeBoundsDoesNotIntersect()
	{
		Assert.False(BoardAsteroidView.SegmentIntersectsBounds(
			new Vector3(-2f, 1f, 1f),
			new Vector3(-1f, 1f, 1f),
			Bounds));
	}

	[Fact]
	public void ParallelSegmentOutsideBoundsDoesNotIntersect()
	{
		Assert.False(BoardAsteroidView.SegmentIntersectsBounds(
			new Vector3(-2f, 3f, 1f),
			new Vector3(4f, 3f, 1f),
			Bounds));
	}

	[Fact]
	public void SegmentEndingInsideBoundsIntersects()
	{
		Assert.True(BoardAsteroidView.SegmentIntersectsBounds(
			new Vector3(-2f, 1f, 1f),
			Vector3.One,
			Bounds));
	}
}

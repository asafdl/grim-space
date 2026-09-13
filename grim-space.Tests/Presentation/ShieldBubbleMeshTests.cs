using Godot;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Presentation;

public sealed class ShieldBubbleMeshTests
{
	private static readonly Aabb Bounds =
		new(new Vector3(-2f, -1f, -3f), new Vector3(4f, 2f, 6f));

	[Theory]
	[InlineData(ESpatialOrientation.Forward, 2, 3.01f)]
	[InlineData(ESpatialOrientation.Retro, 2, -3.01f)]
	[InlineData(ESpatialOrientation.Dorsal, 1, 1.01f)]
	[InlineData(ESpatialOrientation.Ventral, 1, -1.01f)]
	[InlineData(ESpatialOrientation.Port, 0, -2.01f)]
	[InlineData(ESpatialOrientation.Starboard, 0, 2.01f)]
	public void PrepareFace_OffsetsFacetOutsideVisualBounds(
		ESpatialOrientation face,
		int axis,
		float expectedOutside)
	{
		var vertices = ShieldBubbleMesh.PrepareFace(Bounds, face);

		Assert.Equal(112, vertices.Length);
		Assert.All(vertices, vertex =>
		{
			var coordinate = axis switch
			{
				0 => vertex.X,
				1 => vertex.Y,
				_ => vertex.Z,
			};
			Assert.True(
				expectedOutside > 0f
					? coordinate > 0f
					: coordinate < 0f);
		});
		Assert.Contains(vertices, vertex =>
		{
			var coordinate = axis switch
			{
				0 => vertex.X,
				1 => vertex.Y,
				_ => vertex.Z,
			};
			return expectedOutside > 0f
				? coordinate >= expectedOutside
				: coordinate <= expectedOutside;
		});
	}
}

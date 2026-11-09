using Godot;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Presentation;

[BattleTestSuite]
public sealed class ShieldRenderingTests
{
	[Theory]
	[InlineData(ESpatialOrientation.Forward, 0, 0, 1)]
	[InlineData(ESpatialOrientation.Retro, 0, 0, -1)]
	[InlineData(ESpatialOrientation.Port, -1, 0, 0)]
	[InlineData(ESpatialOrientation.Starboard, 1, 0, 0)]
	[InlineData(ESpatialOrientation.Dorsal, 0, 1, 0)]
	[InlineData(ESpatialOrientation.Ventral, 0, -1, 0)]
	public void SurfaceStaysOnItsFaceAndFitsTheOriginalEnvelope(ESpatialOrientation face, int x, int y, int z)
	{
		var bounds = new Aabb(new Vector3(2, -3, 4), new Vector3(4, 2, 6));
		var axis = new Vector3(x, y, z);
		var radius = ShieldBubbleMesh.Radius(bounds);
		var (vertices, normals) = ShieldBubbleMesh.PrepareSurface(bounds, face);

		Assert.NotEmpty(vertices);
		Assert.Equal(0, vertices.Length % 3);
		Assert.Equal(vertices.Length, normals.Length);
		for (var i = 0; i < vertices.Length; i++)
		{
			var direction = (vertices[i] - bounds.GetCenter()) / radius;
			Assert.InRange(direction.Length(), 0.99999f, 1.00001f);
			var dominant = direction.Abs();
			Assert.True(direction.Dot(axis) >= Mathf.Max(dominant.X, Mathf.Max(dominant.Y, dominant.Z)) - 0.00001f);
			Assert.True(normals[i].IsEqualApprox((direction / radius).Normalized()));
		}
		for (var i = 0; i < vertices.Length; i += 3)
		{
			var winding = (vertices[i + 1] - vertices[i]).Cross(vertices[i + 2] - vertices[i]);
			Assert.True(winding.Dot(normals[i]) < 0, "Godot front faces must be clockwise.");
		}
	}

	[Theory]
	[InlineData(ETeam.Player)]
	[InlineData(ETeam.Enemy)]
	public void DamageDimsAndTemporaryShieldsLightenWithoutReplacingTeamIdentity(ETeam team)
	{
		var tint = ShieldFaceMaterials.TeamTint(team);
		var full = ShieldFaceMaterials.TintFor(tint, 100, 100, 0);
		var weak = ShieldFaceMaterials.TintFor(tint, 10, 100, 0);
		var temporary = ShieldFaceMaterials.TintFor(tint, 0, 100, 20);

		Assert.Equal(tint, full);
		Assert.True(weak.R < full.R && weak.G < full.G && weak.B < full.B);
		Assert.True(temporary.R > full.R && temporary.G > full.G && temporary.B > full.B);
		Assert.Equal(team == ETeam.Player, full.B > full.R);
		Assert.Equal(team == ETeam.Player, weak.B > weak.R);
		Assert.Equal(team == ETeam.Player, temporary.B > temporary.R);
	}

	[Fact]
	public void UnassignedViewsAreNeutralAndZeroMaximaDoNotProduceInvalidTints()
	{
		var tint = ShieldFaceMaterials.TeamTint(null);
		Assert.NotEqual(ShieldFaceMaterials.TeamTint(ETeam.Player), tint);
		Assert.NotEqual(ShieldFaceMaterials.TeamTint(ETeam.Enemy), tint);
		Assert.Equal(ShieldFaceMaterials.TintFor(tint, 0, 0, 0),
			ShieldFaceMaterials.TintFor(tint, -10, 100, 0));
	}
}

using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Units;

public sealed class FaceShieldPointsTests
{
	[Fact]
	public void MaxFor_Patrol_HasForwardShieldsOnly()
	{
		var max = FaceShieldPoints.MaxFor(EType.Patrol);

		Assert.Equal(3, max[ESpatialOrientation.Forward]);
		Assert.Equal(0, max[ESpatialOrientation.Retro]);
		Assert.Equal(0, max[ESpatialOrientation.Dorsal]);
		Assert.Equal(0, max[ESpatialOrientation.Ventral]);
		Assert.Equal(0, max[ESpatialOrientation.Port]);
		Assert.Equal(0, max[ESpatialOrientation.Starboard]);
	}

	[Fact]
	public void FromSpawn_Patrol_ClonesForwardShieldProfile()
	{
		var state = State.FromSpawn(
			new Instance { Id = "patrol-1", Type = EType.Patrol, Alliance = Alliance.Enemy },
			Coord.Zero);

		Assert.Equal(3, state.ShieldPoints[ESpatialOrientation.Forward]);
		Assert.Equal(0, state.ShieldPoints[ESpatialOrientation.Retro]);
	}

	[Fact]
	public void MaxFor_FighterAndCarrier_FillAllFaces()
	{
		foreach (var type in new[] { EType.Fighter, EType.Carrier })
		{
			var max = FaceShieldPoints.MaxFor(type);
			foreach (var face in Enum.GetValues<ESpatialOrientation>())
				Assert.Equal(2, max[face]);
		}
	}
}

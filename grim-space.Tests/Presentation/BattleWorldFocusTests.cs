using Godot;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Presentation.Camera;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Presentation;

public sealed class BattleWorldFocusTests
{
	[Fact]
	public void PlayerAftPoseIsBehindAndAboveShip()
	{
		var state = BattleTestFixture.Player(new Coord(5, 5, 5)).State;
		var fore = ToVector(state.Fore);
		var dorsal = ToVector(state.Dorsal);

		var pose = BattleCameraPoses.PlayerAft(state);
		var direction = pose.Offset().Normalized();

		Assert.Equal(WorldMapping.ToWorld(state.Position), pose.Pivot);
		Assert.True(direction.Dot(-fore) > 0.9f);
		Assert.True(direction.Dot(dorsal) > 0f);
		Assert.True(Mathf.IsZeroApprox(direction.Dot(fore.Cross(dorsal))));
	}

	private static Vector3 ToVector(Coord coord) =>
		new(coord.X, coord.Y, coord.Z);
}

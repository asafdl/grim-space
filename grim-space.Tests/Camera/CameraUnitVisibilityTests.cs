using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Presentation.Camera;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Camera;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Units.Loadouts.Defenses;
using GrimSpace.Units.Maneuvering;
using Godot;
using Xunit;

namespace GrimSpace.Tests.Camera;

[BattleTestSuite]
public sealed class CameraUnitVisibilityTests
{
	private static Vector3 ToVector3(Coord coord) => new(coord.X, coord.Y, coord.Z);

	private static State CreateTestState(string id, Coord position, Coord fore, Coord dorsal)
	{
		var emptyShields = new FaceShieldPoints();
		var emptyAbilities = new List<InstalledAbility>();
		
		var state = new State
		{
			Id = id,
			Position = position,
			Fore = fore,
			Dorsal = dorsal,
			Starboard = Coord.Cross(dorsal, fore),
			Type = EType.Fighter,
			Initiative = 0,
			Loadout = new ShipLoadout(100, emptyShields, emptyAbilities, emptyShields),
			Maneuverability = new ManeuverabilitySpec(
				2, 2, 
				new Dictionary<ESpatialOrientation, int>(),
				new Dictionary<EHeadingTurn, int>(),
				new Dictionary<ERollDirection, int>()),
		};
		return state;
	}

	[Fact]
	public void PlayerAftCameraPosesBehindPlayerShip()
	{
		var state = CreateTestState("test-player", new Coord(10, 32, 32), Coord.Forward, Coord.Up);
		
		var pose = BattleCameraPoses.PlayerAft(state);
		
		// Camera should be behind the ship (negative Fore direction)
		var cameraPos = pose.CameraPosition();
		var pivot = pose.Pivot;
		
		var toCamera = cameraPos - pivot;
		var forward = ToVector3(state.Fore);
		
		// Camera should be behind the ship (dot product with forward should be negative)
		var dot = toCamera.X * forward.X + toCamera.Y * forward.Y + toCamera.Z * forward.Z;
		var toCameraLen = Mathf.Sqrt(toCamera.X * toCamera.X + toCamera.Y * toCamera.Y + toCamera.Z * toCamera.Z);
		var forwardLen = Mathf.Sqrt(forward.X * forward.X + forward.Y * forward.Y + forward.Z * forward.Z);
		var cosAngle = dot / (toCameraLen * forwardLen);
		
		Assert.True(cosAngle < -0.5, $"Camera should be behind ship. cosAngle={cosAngle}, toCamera={toCamera}, forward={forward}");
		
		// Distance should be ~18
		Assert.Equal(18f, pose.Distance, 1f);
	}

	[Fact]
	public void CameraLooksAtPivot()
	{
		var state = CreateTestState("test-player", new Coord(10, 32, 32), Coord.Forward, Coord.Up);
		
		var pose = BattleCameraPoses.PlayerAft(state);
		
		// The camera position should be Pivot + direction * Distance
		var cameraPos = pose.CameraPosition();
		var direction = new Vector3(
			Mathf.Cos(pose.Pitch) * Mathf.Sin(pose.Yaw),
			Mathf.Sin(pose.Pitch),
			Mathf.Cos(pose.Pitch) * Mathf.Cos(pose.Yaw));
		
		var expectedPos = pose.Pivot + direction * pose.Distance;
		var diff = cameraPos - expectedPos;
		var diffLen = Mathf.Sqrt(diff.X * diff.X + diff.Y * diff.Y + diff.Z * diff.Z);
		Assert.True(diffLen < 0.01f, 
			$"Camera position mismatch. Actual={cameraPos}, Expected={expectedPos}, diff={diffLen}");
	}

	[Fact]
	public void PlayerAftCameraOrientationCorrectForDifferentForeVectors()
	{
		// Test different orientations - all with valid perpendicular Dorsal
		var testCases = new[]
		{
			new { Fore = Coord.Forward, Dorsal = Coord.Up, Name = "Forward" },
			new { Fore = -Coord.Forward, Dorsal = Coord.Up, Name = "Back" },
			new { Fore = new Coord(1, 0, 0), Dorsal = Coord.Up, Name = "Right" },
			new { Fore = new Coord(-1, 0, 0), Dorsal = Coord.Up, Name = "Left" },
			new { Fore = Coord.Up, Dorsal = new Coord(1, 0, 0), Name = "Up" },
			new { Fore = -Coord.Up, Dorsal = new Coord(1, 0, 0), Name = "Down" },
		};

		foreach (var tc in testCases)
		{
			var state = CreateTestState("test-player", new Coord(10, 32, 32), tc.Fore, tc.Dorsal);
			
			var pose = BattleCameraPoses.PlayerAft(state);
			var cameraPos = pose.CameraPosition();
			var toCamera = cameraPos - pose.Pivot;
			var forward = ToVector3(state.Fore);
			
			var dot = toCamera.X * forward.X + toCamera.Y * forward.Y + toCamera.Z * forward.Z;
			var toCameraLen = Mathf.Sqrt(toCamera.X * toCamera.X + toCamera.Y * toCamera.Y + toCamera.Z * toCamera.Z);
			var forwardLen = Mathf.Sqrt(forward.X * forward.X + forward.Y * forward.Y + forward.Z * forward.Z);
			var cosAngle = dot / (toCameraLen * forwardLen);
			
			// Camera should be behind ship (cosAngle < 0). For Up/Down orientations with dorsal offset,
			// camera may be more to the side. Use lenient threshold.
			Assert.True(cosAngle < -0.2, $"For Fore={tc.Name} ({tc.Fore}), Dorsal={tc.Dorsal}, camera should be behind ship. cosAngle={cosAngle}, toCamera={toCamera}, forward={forward}");
		}
	}

	[Fact]
	public void CameraPivotIsAtPlayerShipPosition()
	{
		var state = CreateTestState("test-player", new Coord(20, 32, 32), Coord.Forward, Coord.Up);
		
		var pose = BattleCameraPoses.PlayerAft(state);
		var expectedPivot = WorldMapping.ToWorld(state.Position);
		
		Assert.Equal(expectedPivot, pose.Pivot);
	}
}
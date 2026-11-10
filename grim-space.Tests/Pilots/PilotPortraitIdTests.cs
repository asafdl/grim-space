using GrimSpace.Pilots;
using GrimSpace.Run;
using GrimSpace.Run.Persistence;
using GrimSpace.Ui.Portraits;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Pilots;

public sealed class PilotPortraitIdTests
{
	[Fact]
	public void Roll_UsesSeededRandomAndIsNotLimitedToLegacyPool()
	{
		var first = new Random(42);
		var second = new Random(42);
		var ids = Enumerable.Range(0, 1000).Select(_ => PilotPortraitId.Roll(first)).ToArray();

		Assert.Equal(ids, Enumerable.Range(0, 1000).Select(_ => PilotPortraitId.Roll(second)));
		Assert.Equal(1000, ids.Distinct().Count());
		Assert.All(ids, id =>
		{
			Assert.StartsWith("robot_v1:", id, StringComparison.Ordinal);
			Assert.InRange(PilotPortraitId.SeedFor(id), 0, int.MaxValue);
		});
	}

	[Theory]
	[InlineData("robot_v1:0", 0)]
	[InlineData("robot_v1:2147483647", int.MaxValue)]
	[InlineData("pilot_01", 1)]
	[InlineData("pilot_09", 9)]
	[InlineData("pilot_10", 10)]
	public void SeedFor_ResolvesPersistedAndLegacyIds(string id, int expected) =>
		Assert.Equal(expected, PilotPortraitId.SeedFor(id));

	[Theory]
	[InlineData("")]
	[InlineData("robot_v1:")]
	[InlineData("robot_v1:-1")]
	[InlineData("robot_v1:2147483648")]
	[InlineData("robot_v1: 42")]
	[InlineData("robot_v2:42")]
	[InlineData("pilot_00")]
	[InlineData("pilot_11")]
	[InlineData("pilot_1")]
	[InlineData("unknown")]
	public void SeedFor_RejectsInvalidIds(string id) =>
		Assert.Throws<ArgumentException>(() => PilotPortraitId.SeedFor(id));

	[Fact]
	public void RecruitHireAndSaveLoad_PreserveAvatarIdentityAndAppearance()
	{
		var id = PilotPortraitId.Roll(new Random(42));
		var profile = new PilotRecruitProfile("Test Robot", id, EPilotBackgroundId.ScrapDrone);
		var pilot = Pilot.FromRecruitProfile("ship-1", profile);
		var serialized = ReflectionJson.Write(pilot.ToPersistence());
		var restored = Pilot.FromPersistence(ReflectionJson.Read<PilotPersistenceDto>(serialized));
		var clone = restored.Clone();
		var reassigned = restored.WithAssignment("ship-2");

		foreach (var copy in new[] { pilot, restored, clone, reassigned })
		{
			Assert.Equal(id, copy.PortraitId);
			Assert.Equal(
				RobotPortraitGenerator.Generate(PilotPortraitId.SeedFor(id)),
				RobotPortraitGenerator.Generate(PilotPortraitId.SeedFor(copy.PortraitId)));
		}
	}
}

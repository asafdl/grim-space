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
		var profile = new PilotRecruitProfile("Test Robot", id, EPilotBackgroundId.ScrapDrone, nickname: "Bolt");
		var pilot = Pilot.FromRecruitProfile("ship-1", profile);
		var serialized = ReflectionJson.Write(pilot.ToPersistence());
		var restored = Pilot.FromPersistence(ReflectionJson.Read<PilotPersistenceDto>(serialized));
		var clone = restored.Clone();
		var reassigned = restored.WithAssignment("ship-2");

		foreach (var copy in new[] { pilot, restored, clone, reassigned })
		{
			Assert.Equal(id, copy.PortraitId);
			Assert.Equal("Bolt", copy.Nickname);
			Assert.Equal(
				PilotPortraitId.GenerateAppearance(id),
				PilotPortraitId.GenerateAppearance(copy.PortraitId));
		}
	}

	[Fact]
	public void WithRandomOtherPalette_KeepsSeedAndChangesOnlyColors()
	{
		var baseId = PilotPortraitId.Roll(new Random(99));
		var original = PilotPortraitId.GenerateAppearance(baseId);
		var nextId = PilotPortraitId.WithRandomOtherPalette(baseId, new Random(5));
		var next = PilotPortraitId.GenerateAppearance(nextId);

		Assert.Equal(PilotPortraitId.SeedFor(baseId), PilotPortraitId.SeedFor(nextId));
		Assert.Equal(original.Parts, next.Parts);
		Assert.NotEqual(original.Colors, next.Colors);
		Assert.True(PilotPortraitId.TryGetPaletteIndex(nextId, out var paletteIndex));
		Assert.InRange(paletteIndex, 0, RobotPortraitGenerator.Palettes.Count - 1);
	}

	[Fact]
	public void GenerateAppearance_PaletteOverrideReplacesColorsOnly()
	{
		var seedId = "robot_v1:42";
		var baseline = RobotPortraitGenerator.Generate(42);
		var overridden = PilotPortraitId.GenerateAppearance(PilotPortraitId.WithPalette(seedId, 0));

		Assert.Equal(baseline.Parts, overridden.Parts);
		Assert.Equal(RobotPortraitGenerator.Palettes[0].Colors, overridden.Colors);
	}
}

using GrimSpace.Battle.PilotCapabilities;
using GrimSpace.Tests.Run;
using GrimSpace.Pilots;
using GrimSpace.Run;
using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem.PilotCapabilities;

namespace GrimSpace.Tests.Pilots;

[IntegrationTestSuite]
public sealed class PilotSkillTreeTests
{
	[Fact]
	public void Catalog_EverySkillGrantsSupportedCapability()
	{
		foreach (var skill in PilotSkillCatalog.All)
		{
			Assert.NotEmpty(skill.CapabilityIds);
			Assert.All(
				skill.CapabilityIds,
				capabilityId => Assert.True(
					BattlePilotCapabilityPipelineFactory.Supports(capabilityId)
					|| StarMapPilotCapabilityPipelineFactory.Supports(capabilityId)));
		}
	}

	[Fact]
	public void Progression_GrantsXpWithoutAutoLearning()
	{
		var pilot = PilotProgression.AddExperience(Pilot.CreateForShip("fighter-a"), 500);

		Assert.Empty(pilot.LearnedSkills);
		Assert.True(PilotProgression.LevelFor(pilot.ExperiencePoints) > pilot.ClaimedLevel);
	}

	[Fact]
	public void Learn_ConsumesOneUnclaimedLevelPick()
	{
		var pilot = PilotProgression.AddExperience(Pilot.CreateForShip("fighter-a"), 250);
		var available = PilotSkillAvailability.AvailableToLearn(pilot);
		Assert.NotEmpty(available);
		var pick = available[0];

		pilot = pilot.LearnPilotSkill(pick);

		Assert.Contains(pick, pilot.LearnedSkills);
		Assert.DoesNotContain(pick, PilotSkillAvailability.AvailableToLearn(pilot));
	}

	[Fact]
	public void Learn_RequiresUnclaimedLevel()
	{
		var pilot = Pilot.CreateForShip("fighter-a");

		Assert.Throws<InvalidOperationException>(() =>
			pilot.LearnPilotSkill(EPilotSkillId.ShieldsUp));
	}

	[Fact]
	public void Tree_FiltersByPilotAudience()
	{
		var scrapPilot = PilotFromBackground(EPilotBackgroundId.ScrapDrone);
		var corporatePilot = PilotFromBackground(EPilotBackgroundId.CorporateSlave);

		var scrapTree = PilotSkillAvailability.TreeFor(scrapPilot).Select(skill => skill.Id);
		var corporateTree = PilotSkillAvailability.TreeFor(corporatePilot).Select(skill => skill.Id);

		Assert.Contains(EPilotSkillId.DronesYearnForMines, scrapTree);
		Assert.DoesNotContain(EPilotSkillId.DronesYearnForMines, corporateTree);
	}

	[Fact]
	public void RunState_TryLearnUpdatesRegistry()
	{
		using var run = State.CreateNewRun(42);
		var pilot = RunTestKit.StarterRangerPilot(run);
		run.PilotRegistry.Update(PilotProgression.AddExperience(pilot, 250));
		Assert.True(run.PilotRegistry.TryGet(pilot.Id, out pilot));

		var available = PilotSkillAvailability.AvailableToLearn(pilot);
		Assert.NotEmpty(available);
		var pick = available[0];
		Assert.True(run.TryLearnPilotSkill(pilot.Id, pick));
		Assert.True(run.PilotRegistry.TryGet(pilot.Id, out var updated));
		Assert.Contains(pick, updated.LearnedSkills);
	}

	[Fact]
	public void IdsForOperatorClass_IncludesGlobalAndClassSkillsOnly()
	{
		var ranger = PilotSkillCatalog.IdsForOperatorClass(EOperatorClassId.SpaceRanger);
		var support = PilotSkillCatalog.IdsForOperatorClass(EOperatorClassId.Support);
		var tank = PilotSkillCatalog.IdsForOperatorClass(EOperatorClassId.Tank);

		Assert.Contains(EPilotSkillId.ReinforcedHull, ranger);
		Assert.Contains(EPilotSkillId.DodgeThis, ranger);
		Assert.DoesNotContain(EPilotSkillId.Brace, ranger);
		Assert.DoesNotContain(EPilotSkillId.DronesYearnForMines, ranger);

		Assert.Contains(EPilotSkillId.Brace, support);
		Assert.Contains(EPilotSkillId.DefensiveManeuvers, support);
		Assert.DoesNotContain(EPilotSkillId.DodgeThis, support);

		Assert.Contains(EPilotSkillId.ReactiveShields, tank);
		Assert.Contains(EPilotSkillId.InTheFray, tank);
		Assert.DoesNotContain(EPilotSkillId.Brace, tank);
	}

	[Fact]
	public void Tree_FiltersSupportClassSkills()
	{
		var supportPilot = Pilot.FromRecruitProfile(
			"miner-a",
			new PilotRecruitProfile(
				"Support Test",
				"pilot_01",
				EPilotBackgroundId.CorporateSlave,
				classId: EOperatorClassId.Support));

		var tree = PilotSkillAvailability.TreeFor(supportPilot).Select(skill => skill.Id).ToArray();

		Assert.Contains(EPilotSkillId.Brace, tree);
		Assert.Contains(EPilotSkillId.DefensiveManeuvers, tree);
		Assert.Contains(EPilotSkillId.ProtectionDrone, tree);
		Assert.DoesNotContain(EPilotSkillId.DodgeThis, tree);
		Assert.DoesNotContain(EPilotSkillId.ReactiveShields, tree);
	}

	[Fact]
	public void TreeFor_TankExcludesOtherClassSkills()
	{
		var tankPilot = Pilot.FromRecruitProfile(
			"gooper-a",
			new PilotRecruitProfile(
				"Tank Test",
				"pilot_01",
				EPilotBackgroundId.CorporateSlave,
				classId: EOperatorClassId.Tank));

		var tree = PilotSkillAvailability.TreeFor(tankPilot).Select(skill => skill.Id).ToArray();

		Assert.Contains(EPilotSkillId.InTheFray, tree);
		Assert.Contains(EPilotSkillId.DefensiveManeuvers, tree);
		Assert.Contains(EPilotSkillId.ReactiveShields, tree);
		Assert.DoesNotContain(EPilotSkillId.DodgeThis, tree);
		Assert.DoesNotContain(EPilotSkillId.Brace, tree);
	}

	private static Pilot PilotFromBackground(EPilotBackgroundId backgroundId) =>
		Pilot.FromRecruitProfile(
			"fighter-a",
			new PilotRecruitProfile(
				"Test Pilot",
				"pilot_01",
				backgroundId,
				classId: EOperatorClassId.SpaceRanger));
}

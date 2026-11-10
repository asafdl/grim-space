using GrimSpace.Battle.PilotCapabilities;
using GrimSpace.Pilots;
using GrimSpace.Run;
using GrimSpace.Run.Items;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem.PilotCapabilities;

namespace GrimSpace.Tests.Run;

[IntegrationTestSuite]
public sealed class PilotRegistryCapabilityQueryTests
{
	[Fact]
	public void QueryCapabilities_WithoutQuirkOrBackgroundGrants_ReturnsSkillsOnly()
	{
		var registry = new RunPilotRegistry();
		var basePilot = Pilot.FromPersistence(
			Pilot.CreateForShip("fighter-a").ToPersistence() with
			{
				BackgroundId = EPilotBackgroundId.OptimalityEnforcer,
				PositiveQuirkIds = [],
				NegativeQuirkIds = [],
			});
		var pilot = PilotProgression
			.AddExperience(basePilot, PilotProgression.ExperienceForLevel(3))
			.LearnPilotSkill(EPilotSkillId.ShieldsUp);
		registry.Register(pilot);

		var grants = registry.QueryCapabilities(BattlePilotCapabilityPipelineFactory.Supports);

		var grant = Assert.Single(grants);
		Assert.Equal(EPilotCapabilitySourceKind.Skill, grant.Source.Kind);
		Assert.Equal(EPilotSkillId.ShieldsUp.ToString(), grant.Source.Id);
	}

	[Fact]
	public void QueryCapabilities_IncludesCharismaticFleetSalaryDiscount()
	{
		var registry = new RunPilotRegistry();
		registry.Register(Pilot.FromPersistence(
			Pilot.CreateForShip("fighter-a").ToPersistence() with
			{
				BackgroundId = EPilotBackgroundId.OptimalityEnforcer,
				PositiveQuirkIds = [EPilotQuirkId.Charismatic],
				NegativeQuirkIds = [],
			}));

		var grants = registry.QueryCapabilities(
			capabilityId => capabilityId == EPilotCapabilityId.FleetSalaryDiscount);

		var grant = Assert.Single(grants);
		Assert.Equal(EPilotCapabilitySourceKind.Quirk, grant.Source.Kind);
		Assert.Equal(EPilotQuirkId.Charismatic.ToString(), grant.Source.Id);
	}

	[Fact]
	public void QueryCapabilities_ConcatenatesSkillsThenBackgroundGrants()
	{
		var registry = new RunPilotRegistry();
		var pilot = Pilot.FromPersistence(
			PilotProgression
				.AddExperience(
					Pilot.CreateForShip("fighter-a"),
					PilotProgression.ExperienceForLevel(PilotProgression.MaxLevel))
				.LearnPilotSkill(EPilotSkillId.ShieldsUp)
				.LearnPilotSkill(EPilotSkillId.IonizedPayload)
				.ToPersistence() with
			{
				BackgroundId = EPilotBackgroundId.ScrapDrone,
				PositiveQuirkIds = [],
				NegativeQuirkIds = [],
			});
		registry.Register(pilot);

		var grants = registry.QueryCapabilities(_ => true);

		Assert.Equal(
			[
				(EPilotCapabilityId.ShieldBoost, EPilotCapabilitySourceKind.Skill),
				(EPilotCapabilityId.LightningShieldDamageBonus, EPilotCapabilitySourceKind.Skill),
				(EPilotCapabilityId.ScrapDroneSwarmDamageBonus, EPilotCapabilitySourceKind.Background),
				(EPilotCapabilityId.MinorScrapRewardBonus, EPilotCapabilitySourceKind.Background),
			],
			grants.Select(grant => (grant.CapabilityId, grant.Source.Kind)).ToArray());
	}

	[Fact]
	public void QueryCapabilities_DuplicateCapabilityFromSkillProducesSingleGrantPerSource()
	{
		var registry = new RunPilotRegistry();
		var pilot = Pilot.FromPersistence(
			PilotProgression
				.AddExperience(
					Pilot.CreateForShip("fighter-a"),
					PilotProgression.ExperienceForLevel(PilotProgression.MaxLevel))
				.LearnPilotSkill(EPilotSkillId.IonizedPayload)
				.ToPersistence() with
			{
				BackgroundId = EPilotBackgroundId.OptimalityEnforcer,
				PositiveQuirkIds = [],
				NegativeQuirkIds = [],
			});
		registry.Register(pilot);

		var battleGrants = registry.QueryCapabilities(BattlePilotCapabilityPipelineFactory.Supports);

		var grant = Assert.Single(battleGrants);
		Assert.Equal(EPilotCapabilityId.LightningShieldDamageBonus, grant.CapabilityId);
		Assert.Equal(EPilotCapabilitySourceKind.Skill, grant.Source.Kind);
	}

	[Fact]
	public void QueryCapabilitiesForAssigned_ExcludesUnassignedPilotGrants()
	{
		var registry = new RunPilotRegistry();
		var assignedPilot = Pilot.FromPersistence(
			PilotProgression
				.AddExperience(
					Pilot.CreateForShip("fighter-assigned"),
					PilotProgression.ExperienceForLevel(3))
				.LearnPilotSkill(EPilotSkillId.ShieldsUp)
				.ToPersistence() with
			{
				BackgroundId = EPilotBackgroundId.OptimalityEnforcer,
				PositiveQuirkIds = [],
				NegativeQuirkIds = [],
			});
		var unassignedPilot = Pilot.FromPersistence(
			Pilot.CreateForShip("fighter-unassigned").ToPersistence() with
			{
				BackgroundId = EPilotBackgroundId.ScrapDrone,
				PositiveQuirkIds = [],
				NegativeQuirkIds = [],
				AssignedShipId = null,
			});
		registry.Register(assignedPilot);
		registry.Register(unassignedPilot);

		var grants = registry.QueryCapabilitiesForAssigned(BattlePilotCapabilityPipelineFactory.Supports);

		Assert.Single(grants);
		Assert.Equal(assignedPilot.Id, grants[0].PilotId);
	}

	[Fact]
	public void QueryCapabilitiesForShip_ReturnsOnlyAssignedPilotGrants()
	{
		var registry = new RunPilotRegistry();
		var pilotA = Pilot.FromPersistence(
			PilotProgression
				.AddExperience(
					Pilot.CreateForShip("fighter-a"),
					PilotProgression.ExperienceForLevel(3))
				.LearnPilotSkill(EPilotSkillId.ShieldsUp)
				.ToPersistence() with
			{
				BackgroundId = EPilotBackgroundId.OptimalityEnforcer,
				PositiveQuirkIds = [],
				NegativeQuirkIds = [],
			});
		var pilotB = Pilot.FromPersistence(
			PilotProgression
				.AddExperience(
					Pilot.CreateForShip("fighter-b"),
					PilotProgression.ExperienceForLevel(PilotProgression.MaxLevel))
				.LearnPilotSkill(EPilotSkillId.IonizedPayload)
				.ToPersistence() with
			{
				BackgroundId = EPilotBackgroundId.OptimalityEnforcer,
				PositiveQuirkIds = [],
				NegativeQuirkIds = [],
			});
		registry.Register(pilotA);
		registry.Register(pilotB);

		var grants = registry.QueryCapabilitiesForShip(
			"fighter-b",
			BattlePilotCapabilityPipelineFactory.Supports);

		var grant = Assert.Single(grants);
		Assert.Equal(pilotB.Id, grant.PilotId);
		Assert.Equal(EPilotCapabilitySourceKind.Skill, grant.Source.Kind);
	}
}

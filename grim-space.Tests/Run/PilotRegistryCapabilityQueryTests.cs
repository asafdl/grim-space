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
	public void QueryCapabilities_QuirksWithoutCapabilityIdsAddNoGrantsBeyondSkills()
	{
		var registry = new RunPilotRegistry();
		var basePilot = Pilot.FromPersistence(
			Pilot.CreateForShip("fighter-a").ToPersistence() with
			{
				BackgroundId = EPilotBackgroundId.OptimalityEnforcer,
			});
		var pilot = PilotProgression
			.AddExperience(basePilot, 250)
			.LearnPilotSkill(EPilotSkillId.ShieldsUp);
		registry.Register(pilot);

		var grants = registry.QueryCapabilities(BattlePilotCapabilityPipelineFactory.Supports);

		var grant = Assert.Single(grants);
		Assert.Equal(EPilotCapabilitySourceKind.Skill, grant.Source.Kind);
		Assert.Equal(EPilotSkillId.ShieldsUp.ToString(), grant.Source.Id);
	}

	[Fact]
	public void QueryCapabilities_ConcatenatesSkillsThenBackgroundGrants()
	{
		var registry = new RunPilotRegistry();
		var pilot = Pilot.FromPersistence(
			PilotProgression
				.AddExperience(Pilot.CreateForShip("fighter-a"), 1000)
				.LearnPilotSkill(EPilotSkillId.ShieldsUp)
				.LearnPilotSkill(EPilotSkillId.IonizedPayload)
				.ToPersistence() with
			{
				BackgroundId = EPilotBackgroundId.ScrapDrone,
				PositiveQuirkId = null,
				NegativeQuirkId = null,
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
				.AddExperience(Pilot.CreateForShip("fighter-a"), 1000)
				.LearnPilotSkill(EPilotSkillId.IonizedPayload)
				.ToPersistence() with
			{
				BackgroundId = EPilotBackgroundId.OptimalityEnforcer,
				PositiveQuirkId = null,
			});
		registry.Register(pilot);

		var battleGrants = registry.QueryCapabilities(BattlePilotCapabilityPipelineFactory.Supports);

		var grant = Assert.Single(battleGrants);
		Assert.Equal(EPilotCapabilityId.LightningShieldDamageBonus, grant.CapabilityId);
		Assert.Equal(EPilotCapabilitySourceKind.Skill, grant.Source.Kind);
	}

	[Fact]
	public void QueryCapabilitiesForActive_ExcludesInactivePilotGrants()
	{
		var registry = new RunPilotRegistry();
		var activePilot = Pilot.FromPersistence(
			PilotProgression
				.AddExperience(Pilot.CreateForShip("fighter-active"), 250)
				.LearnPilotSkill(EPilotSkillId.ShieldsUp)
				.ToPersistence() with
			{
				BackgroundId = EPilotBackgroundId.OptimalityEnforcer,
				PositiveQuirkId = null,
				NegativeQuirkId = null,
				IsActive = false,
			});
		var inactivePilot = Pilot.FromPersistence(
			Pilot.CreateForShip("fighter-inactive").ToPersistence() with
			{
				BackgroundId = EPilotBackgroundId.ScrapDrone,
				PositiveQuirkId = null,
				NegativeQuirkId = null,
				IsActive = false,
			});
		registry.Register(activePilot);
		registry.Register(inactivePilot);
		var inventory = new Inventory();
		inventory.Register(
			ShipInstance.FromCatalog("fighter-active", EType.Fighter),
			State.PlayerFleetUnitId);
		registry.Activate(activePilot.Id, inventory, State.PlayerFleetUnitId);

		var grants = registry.QueryCapabilitiesForActive(BattlePilotCapabilityPipelineFactory.Supports);

		Assert.Single(grants);
		Assert.Equal(activePilot.Id, grants[0].PilotId);
	}

	[Fact]
	public void QueryCapabilitiesForShip_ReturnsOnlyAssignedPilotGrants()
	{
		var registry = new RunPilotRegistry();
		var pilotA = Pilot.FromPersistence(
			PilotProgression
				.AddExperience(Pilot.CreateForShip("fighter-a"), 250)
				.LearnPilotSkill(EPilotSkillId.ShieldsUp)
				.ToPersistence() with
			{
				BackgroundId = EPilotBackgroundId.OptimalityEnforcer,
				PositiveQuirkId = null,
				NegativeQuirkId = null,
			});
		var pilotB = Pilot.FromPersistence(
			PilotProgression
				.AddExperience(Pilot.CreateForShip("fighter-b"), 1000)
				.LearnPilotSkill(EPilotSkillId.IonizedPayload)
				.ToPersistence() with
			{
				BackgroundId = EPilotBackgroundId.OptimalityEnforcer,
				PositiveQuirkId = null,
				NegativeQuirkId = null,
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

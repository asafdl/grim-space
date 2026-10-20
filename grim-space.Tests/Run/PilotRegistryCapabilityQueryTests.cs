using GrimSpace.Battle.PilotCapabilities;
using GrimSpace.Pilots;
using GrimSpace.Run;
using GrimSpace.World.StarSystem.PilotCapabilities;

namespace GrimSpace.Tests.Run;

[IntegrationTestSuite]
public sealed class PilotRegistryCapabilityQueryTests
{
	[Fact]
	public void QueryCapabilities_PlaceholderQuirksAddNoGrantsBeyondSkills()
	{
		var registry = new RunPilotRegistry();
		var pilot = PilotProgression
			.AddExperience(Pilot.CreateForShip("fighter-a"), 100)
			.LearnPilotSkill(EPilotSkillId.ShieldsUp);
		registry.Register(pilot);

		var grants = registry.QueryCapabilities(BattlePilotCapabilityPipelineFactory.Supports);

		var grant = Assert.Single(grants);
		Assert.Equal(EPilotCapabilitySourceKind.Skill, grant.Source.Kind);
		Assert.Equal(EPilotSkillId.ShieldsUp.ToString(), grant.Source.Id);
	}

	[Fact]
	public void QueryCapabilities_ConcatenatesSkillsThenPositiveThenNegativeQuirkGrants()
	{
		var registry = new RunPilotRegistry();
		var pilot = Pilot.FromPersistence(
			PilotProgression
				.AddExperience(Pilot.CreateForShip("fighter-a"), 250)
				.LearnPilotSkill(EPilotSkillId.ShieldsUp)
				.LearnPilotSkill(EPilotSkillId.IonizedPayload)
				.ToPersistence() with
			{
				PositiveQuirkId = EPilotQuirkId.GrantProbePositive,
				NegativeQuirkId = EPilotQuirkId.GrantProbeNegative,
			});
		registry.Register(pilot);

		var grants = registry.QueryCapabilities(_ => true);

		Assert.Equal(
			[
				(EPilotCapabilityId.ShieldBoost, EPilotCapabilitySourceKind.Skill),
				(EPilotCapabilityId.LightningShieldDamageBonus, EPilotCapabilitySourceKind.Skill),
				(EPilotCapabilityId.LightningShieldDamageBonus, EPilotCapabilitySourceKind.Quirk),
				(EPilotCapabilityId.ScrapRewardBonus, EPilotCapabilitySourceKind.Quirk),
			],
			grants.Select(grant => (grant.CapabilityId, grant.Source.Kind)).ToArray());
	}

	[Fact]
	public void QueryCapabilities_DuplicateCapabilityFromSkillAndQuirkProducesTwoGrants()
	{
		var registry = new RunPilotRegistry();
		var pilot = Pilot.FromPersistence(
			PilotProgression
				.AddExperience(Pilot.CreateForShip("fighter-a"), 250)
				.LearnPilotSkill(EPilotSkillId.IonizedPayload)
				.ToPersistence() with
			{
				PositiveQuirkId = EPilotQuirkId.GrantProbePositive,
			});
		registry.Register(pilot);

		var battleGrants = registry.QueryCapabilities(BattlePilotCapabilityPipelineFactory.Supports);

		Assert.Equal(2, battleGrants.Count);
		Assert.All(battleGrants, g => Assert.Equal(EPilotCapabilityId.LightningShieldDamageBonus, g.CapabilityId));
		Assert.Contains(
			battleGrants,
			g => g.Source.Kind == EPilotCapabilitySourceKind.Skill
				&& g.Source.Id == EPilotSkillId.IonizedPayload.ToString());
		Assert.Contains(
			battleGrants,
			g => g.Source.Kind == EPilotCapabilitySourceKind.Quirk
				&& g.Source.Id == EPilotQuirkId.GrantProbePositive.ToString());
	}

	[Fact]
	public void QueryCapabilitiesForActive_ExcludesInactivePilotQuirkGrants()
	{
		var registry = new RunPilotRegistry();
		var activePilot = Pilot.FromPersistence(
			Pilot.CreateForShip("fighter-active").ToPersistence() with
			{
				PositiveQuirkId = EPilotQuirkId.GrantProbePositive,
			});
		var inactivePilot = Pilot.FromPersistence(
			Pilot.CreateForShip("fighter-inactive").ToPersistence() with
			{
				PositiveQuirkId = EPilotQuirkId.GrantProbePositive,
			});
		registry.Register(activePilot.WithActive(true));
		registry.Register(inactivePilot);

		var grants = registry.QueryCapabilitiesForActive(BattlePilotCapabilityPipelineFactory.Supports);

		Assert.Single(grants);
		Assert.Equal(activePilot.Id, grants[0].PilotId);
	}

	[Fact]
	public void QueryCapabilitiesForShip_ReturnsOnlyAssignedPilotQuirkGrants()
	{
		var registry = new RunPilotRegistry();
		var pilotA = Pilot.FromPersistence(
			Pilot.CreateForShip("fighter-a").ToPersistence() with
			{
				PositiveQuirkId = EPilotQuirkId.GrantProbePositive,
			});
		var pilotB = Pilot.FromPersistence(
			Pilot.CreateForShip("fighter-b").ToPersistence() with
			{
				PositiveQuirkId = EPilotQuirkId.GrantProbePositive,
			});
		registry.Register(pilotA);
		registry.Register(pilotB);

		var grants = registry.QueryCapabilitiesForShip(
			"fighter-b",
			BattlePilotCapabilityPipelineFactory.Supports);

		var grant = Assert.Single(grants);
		Assert.Equal(pilotB.Id, grant.PilotId);
		Assert.Equal(EPilotCapabilitySourceKind.Quirk, grant.Source.Kind);
	}
}

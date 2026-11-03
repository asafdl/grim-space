using System.Text.Json;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;
using GrimSpace.Run;
using GrimSpace.Run.Persistence;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Units.Maneuvering;
using GrimSpace.Units.Specs;

namespace GrimSpace.Tests.Units;

public sealed class RogueSpecTests
{
	[Fact]
	public void CatalogCreatesAgreedBaseChassisWithBothAbilities()
	{
		var ship = ShipCatalog.CreateInstance("rogue", EType.Rogue);

		Assert.Same(RogueSpec.Instance, ship.Spec);
		Assert.Equal(EOperatorClassId.Rogue, ship.Spec.ClassId);
		Assert.Equal(50, ship.Spec.Initiative);
		Assert.Equal(120, ship.Spec.DefaultMaxHullPoints);
		Assert.Equal(120, ship.Loadout.MaxHullPoints);
		Assert.Equal(120, ship.HullPoints);
		Assert.Equal(3, ship.Spec.Slots.Count);
		Assert.Equal(3, ship.Loadout.InstalledAbilities.Count);
		Assert.Equal(
			new[] { ESpatialOrientation.Ventral, ESpatialOrientation.Dorsal },
			ship.Loadout.InstalledAbilities
				.Where(ability => ability.Kind == EAbilityKind.HighSpeedRod)
				.Select(ability => ability.MountedOn));
		Assert.Single(ship.Loadout.InstalledAbilities,
			ability => ability.Kind == EAbilityKind.RoadsWeDontNeedRoads);
		foreach (var face in Enum.GetValues<ESpatialOrientation>())
		{
			var expected = face == ESpatialOrientation.Retro ? 0 : 50;
			Assert.Equal(expected, ship.Spec.DefaultMaxShieldPoints[face]);
			Assert.Equal(expected, ship.Loadout.MaxShieldPoints[face]);
			Assert.Equal(expected, ship.ShieldPoints[face]);
		}
	}

	[Fact]
	public void MovementUsesOneApInEveryDirectionAndStandardRotationCosts()
	{
		var profile = RogueSpec.Instance.Maneuverability;
		var fighter = FighterSpec.Instance.Maneuverability;

		Assert.Equal(4, profile.MaxActionPoints);
		Assert.Equal(4, profile.MaxManeuverPoints);
		foreach (var face in Enum.GetValues<ESpatialOrientation>())
		{
			Assert.True(profile.TryGetTranslationApCost(face, out var cost));
			Assert.Equal(1, cost);
		}
		Assert.True(fighter.TryGetTranslationApCost(ESpatialOrientation.Retro, out var reverseCost));
		Assert.Equal(2, reverseCost);
		foreach (var turn in Enum.GetValues<EHeadingTurn>())
		{
			Assert.True(profile.TryGetHeadingMpCost(turn, out var cost));
			Assert.Equal(turn == EHeadingTurn.Yaw180 ? 2 : 1, cost);
		}
		foreach (var roll in Enum.GetValues<ERollDirection>())
		{
			Assert.True(profile.TryGetRollMpCost(roll, out var cost));
			Assert.Equal(1, cost);
		}
	}

	[Fact]
	public void ClassHasDisplayNameSlugAndExclusivePilotCompatibility()
	{
		Assert.Equal("Rogue", EOperatorClassId.Rogue.DisplayName());
		Assert.Equal("rogue", UnitTypeSlug.For(EType.Rogue));
		foreach (var classId in Enum.GetValues<EOperatorClassId>())
		{
			Assert.Equal(
				classId == EOperatorClassId.Rogue,
				OperatorClassCompatibility.CanOperate(classId, EOperatorClassId.Rogue));
			Assert.Equal(
				classId == EOperatorClassId.Rogue,
				OperatorClassCompatibility.CanOperate(EOperatorClassId.Rogue, classId));
		}
	}

	[Theory]
	[InlineData(EShipGearTier.T0)]
	[InlineData(EShipGearTier.T1)]
	[InlineData(EShipGearTier.T2)]
	[InlineData(EShipGearTier.T3)]
	public void GearTiersPreserveChassisAbilities(EShipGearTier tier)
	{
		var loadout = ShipCatalog.LoadoutForTier(EType.Rogue, tier, rollSeed: 42);

		Assert.Equal(3, loadout.InstalledAbilities.Count);
		Assert.Equal(tier, ShipLoadoutGearTier.FromLoadout(loadout, EType.Rogue));
	}

	[Fact]
	public void GearBonusesIncreaseCapacityAndCanAddRetroShields()
	{
		var spec = RogueSpec.Instance;
		var baseline = spec.NewDefaultLoadout();
		var upgraded = ChassisBuffCatalog.Apply(
			spec,
			baseline,
			[EChassisBuffOffer.ShieldFlat, EChassisBuffOffer.HullPercent]);

		Assert.Equal(138, upgraded.MaxHullPoints);
		foreach (var face in Enum.GetValues<ESpatialOrientation>())
			Assert.Equal(face == ESpatialOrientation.Retro ? 30 : 80, upgraded.MaxShieldPoints[face]);
		Assert.Equal(120, baseline.MaxHullPoints);
		Assert.Equal(0, baseline.MaxShieldPoints[ESpatialOrientation.Retro]);
	}

	[Fact]
	public void ShieldPercentageBonusesHaveNoSeparateChassisCeiling()
	{
		var spec = RogueSpec.Instance;
		var upgraded = ChassisBuffCatalog.Apply(
			spec,
			spec.NewDefaultLoadout(),
			Enumerable.Repeat(EChassisBuffOffer.ShieldPercentHeavy, 25).ToArray());

		foreach (var face in Enum.GetValues<ESpatialOrientation>())
			Assert.Equal(face == ESpatialOrientation.Retro ? 0 : 550, upgraded.MaxShieldPoints[face]);
	}

	[Fact]
	public void SaveRoundTripPreservesGearCapacityAndDamagedCondition()
	{
		var spec = RogueSpec.Instance;
		var loadout = ChassisBuffCatalog.Apply(
			spec,
			spec.NewDefaultLoadout(),
			[EChassisBuffOffer.ShieldFlat, EChassisBuffOffer.HullPercent]);
		var ship = ShipInstance.FromSpec("rogue", spec, loadout);
		ship.HullPoints = 75;
		ship.ShieldPoints[ESpatialOrientation.Forward] = 12;

		var options = PersistenceJson.CreateOptions();
		var json = JsonSerializer.Serialize(ship, options);
		var restored = JsonSerializer.Deserialize<ShipInstance>(json, options)!;

		Assert.Same(spec, restored.Spec);
		Assert.Equal(138, restored.Loadout.MaxHullPoints);
		Assert.Equal(75, restored.HullPoints);
		Assert.Equal(loadout.Buffs, restored.Loadout.Buffs);
		foreach (var face in Enum.GetValues<ESpatialOrientation>())
		{
			Assert.Equal(loadout.MaxShieldPoints[face], restored.Loadout.MaxShieldPoints[face]);
			Assert.Equal(ship.ShieldPoints[face], restored.ShieldPoints[face]);
		}
	}

	[Fact]
	public void PilotClassSurvivesPersistence()
	{
		var pilot = Pilot.FromRecruitProfile(
			null,
			new PilotRecruitProfile(
				"Rogue Pilot",
				"pilot_01",
				EPilotBackgroundId.ScrapDrone,
				classId: EOperatorClassId.Rogue));
		var options = PersistenceJson.CreateOptions();
		var json = JsonSerializer.Serialize(pilot.ToPersistence(), options);
		var dto = JsonSerializer.Deserialize<PilotPersistenceDto>(json, options)!;
		var restored = Pilot.FromPersistence(dto);

		Assert.Equal(EOperatorClassId.Rogue, restored.ClassId);
		Assert.Equal(pilot.Id, restored.Id);
	}

	[Fact]
	public void ChassisCapabilitiesIncludeBothAbilities()
	{
		Assert.Equal(2, Capabilities.AbilitiesFor(EType.Rogue).Count);
		Assert.Equal(5, Capabilities.For(EType.Rogue).Count);
	}
}

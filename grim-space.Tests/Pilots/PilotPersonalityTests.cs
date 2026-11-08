using GrimSpace.Pilots;
using GrimSpace.Run;
using GrimSpace.Run.Persistence;
using GrimSpace.Tests.Run;
using GrimSpace.Components;

namespace GrimSpace.Tests.Pilots;

[IntegrationTestSuite]
public sealed class PilotPersonalityTests
{
	[Fact]
	public void ResponseWeight_HandlesEveryAxisStrengthAndSide()
	{
		foreach (var axis in Enum.GetValues<EPilotPersonalityAxis>())
		for (var strength = -5; strength <= 5; strength++)
		{
			var personality = axis switch
			{
				EPilotPersonalityAxis.ThreatResponse => new PilotPersonality(strength, 0, 0),
				EPilotPersonalityAxis.Deliberation => new PilotPersonality(0, strength, 0),
				_ => new PilotPersonality(0, 0, strength),
			};
			Assert.Equal(strength == 0 ? 1 : strength > 0 ? strength * strength : 0,
				personality.ResponseWeight(axis, true));
			Assert.Equal(strength == 0 ? 1 : strength < 0 ? strength * strength : 0,
				personality.ResponseWeight(axis, false));
		}
		Assert.Throws<ArgumentOutOfRangeException>(() =>
			default(PilotPersonality).ResponseWeight((EPilotPersonalityAxis)999, true));
	}

	[Fact]
	public void ShiftedChance_SwingsWithAxisAndClampsToUnitInterval()
	{
		var impulsive = new PilotPersonality(0, -5, 0);
		var calculated = new PilotPersonality(0, 5, 0);

		Assert.Equal(0.12, impulsive.ShiftedChance(0.08, 0.04, EPilotPersonalityAxis.Deliberation, false));
		Assert.Equal(0.04, calculated.ShiftedChance(0.08, 0.04, EPilotPersonalityAxis.Deliberation, false));
		Assert.Equal(0.08, default(PilotPersonality).ShiftedChance(0.08, 0.04, EPilotPersonalityAxis.Deliberation, true));
		Assert.Equal(0, impulsive.ShiftedChance(0.01, 0.5, EPilotPersonalityAxis.Deliberation, true));
		Assert.Equal(1, calculated.ShiftedChance(0.9, 0.5, EPilotPersonalityAxis.Deliberation, true));
		Assert.Throws<ArgumentOutOfRangeException>(() =>
			default(PilotPersonality).ShiftedChance(0.5, 0.1, (EPilotPersonalityAxis)999, true));
	}

	[Fact]
	public void SelectResponseIndex_UsesExactWeightedIntervalsForNonDialogueOptions()
	{
		var personality = new PilotPersonality(5, 0, 0);
		var options = new[] { "Confront", "Inspect" };
		static (EPilotPersonalityAxis, bool) Affinity(string option) =>
			(option == "Confront" ? EPilotPersonalityAxis.ThreatResponse : EPilotPersonalityAxis.Deliberation, true);

		for (var roll = 0; roll < 26; roll++)
		{
			var rng = new FixedRollRandom(roll);
			Assert.Equal(roll < 25 ? 0 : 1,
				personality.SelectResponseIndex(options, Affinity, rng));
			Assert.Equal(26, rng.TotalWeight);
		}
	}

	[Fact]
	public void SelectResponseIndex_NeutralIsUniformAndExcludedOptionsHaveNoInterval()
	{
		var options = new[]
		{
			(EPilotPersonalityAxis.ThreatResponse, false),
			(EPilotPersonalityAxis.ThreatResponse, true),
			(EPilotPersonalityAxis.Deliberation, true),
		};
		for (var roll = 0; roll < 3; roll++)
			Assert.Equal(roll, default(PilotPersonality)
				.SelectResponseIndex(options, option => option, new FixedRollRandom(roll)));
		for (var roll = 0; roll < 26; roll++)
			Assert.Equal(roll < 25 ? 1 : 2, new PilotPersonality(5, 0, 0)
				.SelectResponseIndex(options, option => option, new FixedRollRandom(roll)));

		var noRoll = new FixedRollRandom(0);
		Assert.Null(new PilotPersonality(5, 0, 0).SelectResponseIndex(
			[options[0]], option => option, noRoll));
		Assert.Null(noRoll.TotalWeight);
	}

	[Fact]
	public void SelectResponseIndex_SeededDistributionMatchesTwentyFiveToOne()
	{
		var personality = new PilotPersonality(5, 0, 0);
		var options = new[]
		{
			(EPilotPersonalityAxis.ThreatResponse, true),
			(EPilotPersonalityAxis.Deliberation, true),
		};
		var rng = new Random(42);
		var alignedCount = 0;
		for (var index = 0; index < 26000; index++)
			if (personality.SelectResponseIndex(options, option => option, rng) == 0)
				alignedCount++;

		Assert.InRange(alignedCount / 26000.0, 25.0 / 26 - 0.01, 25.0 / 26 + 0.01);
	}

	[Fact]
	public void SelectResponseIndex_RejectsInvalidArguments()
	{
		var personality = default(PilotPersonality);
		var rng = new Random(42);
		Assert.Throws<ArgumentException>(() => personality.SelectResponseIndex(
			Array.Empty<int>(), _ => (EPilotPersonalityAxis.Allegiance, true), rng));
		Assert.Throws<ArgumentNullException>(() => personality.SelectResponseIndex<int>(
			null!, _ => (EPilotPersonalityAxis.Allegiance, true), rng));
		Assert.Throws<ArgumentNullException>(() => personality.SelectResponseIndex([1], null!, rng));
		Assert.Throws<ArgumentNullException>(() => personality.SelectResponseIndex(
			[1], _ => (EPilotPersonalityAxis.Allegiance, true), null!));
	}

	private sealed class FixedRollRandom(int roll) : Random
	{
		public int? TotalWeight { get; private set; }

		public override int Next(int maxValue)
		{
			Assert.InRange(roll, 0, maxValue - 1);
			TotalWeight = maxValue;
			return roll;
		}
	}

	[Fact]
	public void Default_IsNeutral()
	{
		var personality = default(PilotPersonality);

		Assert.Equal(0, personality.ThreatResponse);
		Assert.Equal(0, personality.Deliberation);
		Assert.Equal(0, personality.Allegiance);
		Assert.Equal(new PilotPersonality(0, 0, 0), personality);
	}

	[Fact]
	public void Constructor_PreservesEveryValidCombination()
	{
		for (var threatResponse = -5; threatResponse <= 5; threatResponse++)
		for (var deliberation = -5; deliberation <= 5; deliberation++)
		for (var allegiance = -5; allegiance <= 5; allegiance++)
		{
			var personality = new PilotPersonality(threatResponse, deliberation, allegiance);

			Assert.Equal(threatResponse, personality.ThreatResponse);
			Assert.Equal(deliberation, personality.Deliberation);
			Assert.Equal(allegiance, personality.Allegiance);
		}
	}

	[Theory]
	[InlineData(-6)]
	[InlineData(6)]
	[InlineData(int.MinValue)]
	[InlineData(int.MaxValue)]
	public void Constructor_RejectsOutOfRangeValuesOnEachAxis(int value)
	{
		Assert.Throws<ArgumentOutOfRangeException>("threatResponse", () =>
			new PilotPersonality(value, 0, 0));
		Assert.Throws<ArgumentOutOfRangeException>("deliberation", () =>
			new PilotPersonality(0, value, 0));
		Assert.Throws<ArgumentOutOfRangeException>("allegiance", () =>
			new PilotPersonality(0, 0, value));
	}

	[Fact]
	public void Equality_UsesAllThreeAxes()
	{
		var personality = new PilotPersonality(-2, 3, 4);

		Assert.Equal(new PilotPersonality(-2, 3, 4), personality);
		Assert.NotEqual(new PilotPersonality(-1, 3, 4), personality);
		Assert.NotEqual(new PilotPersonality(-2, 2, 4), personality);
		Assert.NotEqual(new PilotPersonality(-2, 3, 5), personality);
	}

	[Fact]
	public void ApplyModifiers_SumsAllAxesAndPreservesBaseline()
	{
		var baseline = new PilotPersonality(-1, 2, -3);
		var result = baseline.ApplyModifiers(new(2, -3, 1), new(-3, 2, 4));

		Assert.Equal(new PilotPersonality(-2, 1, 2), result);
		Assert.Equal(new PilotPersonality(-1, 2, -3), baseline);
		Assert.Equal(baseline, baseline.ApplyModifiers());
		Assert.Throws<ArgumentNullException>(() => baseline.ApplyModifiers(null!));
	}

	[Fact]
	public void ApplyModifiers_ClampsOnlyAfterSummingRegardlessOfOrder()
	{
		var baseline = new PilotPersonality(4, -4, 4);
		var first = new PilotPersonalityModifier(5, -5, -5);
		var second = new PilotPersonalityModifier(-5, 5, 5);

		Assert.Equal(baseline, baseline.ApplyModifiers(first, second));
		Assert.Equal(baseline, baseline.ApplyModifiers(second, first));
		Assert.Equal(new PilotPersonality(5, -5, -1), baseline.ApplyModifiers(first));
		Assert.Equal(new PilotPersonality(-1, 1, 5), baseline.ApplyModifiers(second));
	}

	[Fact]
	public void DefinitionModifiers_AreOptionalAndShareComposition()
	{
		var tooltip = new HudTooltipContent("Test", "Personality modifier test.");
		var modifier = new PilotPersonalityModifier(-8, 10, 1);
		var background = new PilotBackgroundDefinition(
			EPilotBackgroundId.ScrapDrone, [], tooltip, modifier);
		var quirk = new PilotQuirkDefinition(
			EPilotQuirkId.Charismatic, EPilotQuirkPolarity.Positive, [], tooltip, modifier);

		Assert.Equal(modifier, background.PersonalityModifier);
		Assert.Equal(modifier, quirk.PersonalityModifier);
		Assert.Equal(new PilotPersonality(-5, 5, 2),
			default(PilotPersonality).ApplyModifiers(
				background.PersonalityModifier, quirk.PersonalityModifier));
		Assert.Equal(default,
			new PilotBackgroundDefinition(EPilotBackgroundId.ScrapDrone, [], tooltip).PersonalityModifier);
		Assert.Equal(default,
			new PilotQuirkDefinition(
				EPilotQuirkId.Charismatic, EPilotQuirkPolarity.Positive, [], tooltip).PersonalityModifier);
	}

	[Theory]
	[InlineData(EPilotBackgroundId.GearPsycho)]
	[InlineData(EPilotBackgroundId.BeanCounter)]
	[InlineData(EPilotBackgroundId.ScrapDrone)]
	public void EffectivePersonality_AppliesBackgroundWithoutAccumulation(
		EPilotBackgroundId backgroundId)
	{
		var pilot = Pilot.FromRecruitProfile(null,
			new PilotRecruitProfile("Test", "pilot_01", backgroundId));
		var expected = default(PilotPersonality).ApplyModifiers(
			PilotBackgroundCatalog.Get(backgroundId).PersonalityModifier);

		Assert.Equal(expected, pilot.EffectivePersonality);
		Assert.Equal(expected, pilot.EffectivePersonality);
		Assert.Equal(default, pilot.Personality);
		Assert.Equal(expected, pilot.Clone().EffectivePersonality);
		Assert.Equal(expected, pilot.WithAssignment("ship-1", true).EffectivePersonality);
	}

	[Fact]
	public void ApplyModifiers_PreservesLargeContributionsUntilFinalClamp()
	{
		var baseline = default(PilotPersonality);
		var background = new PilotPersonalityModifier(-5, 5, -5);
		var quirk = new PilotPersonalityModifier(-8, 8, -8);
		var later = new PilotPersonalityModifier(10, -10, 10);

		Assert.Equal(new PilotPersonality(-5, 5, -5), baseline.ApplyModifiers(background, quirk));
		Assert.Equal(new PilotPersonality(-3, 3, -3), baseline.ApplyModifiers(background, quirk, later));
		Assert.Equal(new PilotPersonality(-3, 3, -3), baseline.ApplyModifiers(later, quirk, background));
		Assert.Equal(new PilotPersonality(5, -5, 5), baseline.ApplyModifiers(background, later));
		Assert.Equal(default, baseline);
	}

	[Fact]
	public void ApplyModifiers_SumsFullIntegerRangeWithoutOverflow()
	{
		var positive = new PilotPersonalityModifier(int.MaxValue, int.MinValue, int.MaxValue);
		var negative = new PilotPersonalityModifier(int.MinValue, int.MaxValue, int.MinValue);

		Assert.Equal(new PilotPersonality(5, -5, 5),
			default(PilotPersonality).ApplyModifiers(positive, positive));
		Assert.Equal(new PilotPersonality(-5, 5, -5),
			default(PilotPersonality).ApplyModifiers(negative, negative));
		Assert.Equal(new PilotPersonality(-1, -1, -1),
			default(PilotPersonality).ApplyModifiers(positive, negative));
	}

	[Fact]
	public void EffectivePersonality_UsesCurrentQuirkSlotsWithoutChangingBaseline()
	{
		var baseline = new PilotPersonality(4, -2, 1);
		var withQuirks = Pilot.FromRecruitProfile(null,
			new PilotRecruitProfile("Test", "pilot_01", EPilotBackgroundId.GearPsycho,
				[EPilotQuirkId.Charismatic], [EPilotQuirkId.NeuroDrugAddict], personality: baseline));
		var withoutQuirks = Pilot.FromRecruitProfile(null,
			new PilotRecruitProfile("Test", "pilot_01", EPilotBackgroundId.GearPsycho,
				personality: baseline));
		var expected = baseline.ApplyModifiers(
			PilotBackgroundCatalog.Get(EPilotBackgroundId.GearPsycho).PersonalityModifier,
			PilotQuirkCatalog.Get(EPilotQuirkId.Charismatic).PersonalityModifier,
			PilotQuirkCatalog.Get(EPilotQuirkId.NeuroDrugAddict).PersonalityModifier);

		Assert.Equal(expected, withQuirks.EffectivePersonality);
		Assert.Equal(new PilotPersonality(5, -5, -2), withQuirks.EffectivePersonality);
		Assert.NotEqual(withoutQuirks.EffectivePersonality, withQuirks.EffectivePersonality);
		Assert.Equal(baseline, withQuirks.Personality);
	}

	[Theory]
	[InlineData(EPilotBackgroundId.GearPsycho)]
	[InlineData(EPilotBackgroundId.BeanCounter)]
	public void Persistence_RestoresModifiersWithoutBakingThemIntoBaseline(
		EPilotBackgroundId backgroundId)
	{
		var baseline = new PilotPersonality(4, 4, -2);
		var pilot = Pilot.FromRecruitProfile(null,
			new PilotRecruitProfile("Test", "pilot_01", backgroundId, personality: baseline));
		var options = PersistenceJson.CreateOptions();
		var json = ReflectionJson.Write(pilot.ToPersistence(), options);
		var restored = Pilot.FromPersistence(ReflectionJson.Read<PilotPersistenceDto>(json, options));

		Assert.Equal(baseline, restored.Personality);
		Assert.Equal(pilot.EffectivePersonality, restored.EffectivePersonality);
	}

	[Fact]
	public void PilotCreation_DefaultsToNeutral()
	{
		var profile = new PilotRecruitProfile("Neutral", "pilot_01", EPilotBackgroundId.ScrapDrone);

		Assert.Equal(default, profile.Personality);
		Assert.Equal(default, Pilot.FromRecruitProfile(null, profile).Personality);
		Assert.Equal(default, Pilot.CreateForShip("ship-1", rng: new Random(42)).Personality);
		Assert.Equal(default, Pilot.CreateStarterForShip("ship-2", new Random(42)).Personality);
	}

	[Fact]
	public void FromRecruitProfile_PreservesPersonality()
	{
		var personality = new PilotPersonality(-5, 2, 3);
		var profile = new PilotRecruitProfile(
			"Recruit", "pilot_01", EPilotBackgroundId.ScrapDrone, personality: personality);

		Assert.Equal(personality, Pilot.FromRecruitProfile(null, profile).Personality);
	}

	[Fact]
	public void PilotUpdates_PreservePersonality()
	{
		var personality = new PilotPersonality(-3, 4, -2);
		var original = Pilot.CreateForShip("ship-1", rng: new Random(42));
		var pilot = original.WithPersonality(personality);

		Assert.Equal(default, original.Personality);
		Assert.Equal(personality, pilot.Personality);
		Assert.Equal(personality, pilot.Clone().Personality);
		Assert.Equal(personality, pilot.WithAssignment("ship-2", true).Personality);
		Assert.Equal(personality, pilot.WithActive(true).Personality);
		Assert.Equal(personality, pilot.WithSalaryPayroll(1).Personality);
		Assert.Equal(personality, pilot.WithHazardMissedPeriods(1).Personality);
		Assert.Equal(default, pilot.WithPersonality(default).Personality);
		var experienced = PilotProgression.AddExperience(pilot, PilotProgression.ExperienceForLevel(3));
		Assert.Equal(personality, experienced.Personality);
		Assert.Equal(personality, experienced.LearnPilotSkill(EPilotSkillId.ShieldsUp).Personality);
	}

	[Fact]
	public void RunSave_RoundTripsNonNeutralPersonality()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var pilot = RunTestKit.StarterRangerPilot(run);
		var personality = new PilotPersonality(5, -4, 3);
		run.PilotRegistry.Update(pilot.WithPersonality(personality));
		var registry = PersistenceRegistry.CreateDefault();
		var document = SaveGameDocument.Create(
			"test", "map", run.CaptureSnapshot(registry), options: registry.Options);
		var snapshot = ReflectionJson.Read<RunStateSnapshotDto>(document.Payload, registry.Options);
		using var restored = State.FromSnapshot(snapshot, registry);

		Assert.True(restored.PilotRegistry.TryGet(pilot.Id, out var restoredPilot));
		Assert.Equal(personality, restoredPilot.Personality);
	}

	[Fact]
	public void PersonalityJson_RoundTripsWithBothSerializerOptions()
	{
		var personality = new PilotPersonality(-5, 5, -2);
		foreach (var options in new[] { ReflectionJson.CreateOptions(), PersistenceJson.CreateOptions() })
		{
			var json = ReflectionJson.Write(personality, options);

			Assert.Equal(personality, ReflectionJson.Read<PilotPersonality>(json, options));
			using var invalid = System.Text.Json.JsonDocument.Parse(
				"""{"threatResponse":6,"deliberation":0,"allegiance":0}""");
			Assert.Throws<ArgumentOutOfRangeException>(() =>
				ReflectionJson.Read<PilotPersonality>(invalid.RootElement, options));
		}
	}
}

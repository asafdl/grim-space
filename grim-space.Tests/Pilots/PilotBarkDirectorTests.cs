using GrimSpace.Pilots.Barks;
using GrimSpace.Battle.Effects;
using GrimSpace.Units.Enums;
using GrimSpace.Pilots;
using GrimSpace.Run;
using GrimSpace.Tests.Run;

namespace GrimSpace.Tests.Pilots;

[IntegrationTestSuite]
public sealed class PilotBarkDirectorTests
{
	private readonly List<(string PilotId, string Line)> _said = [];
	private bool _canShow = true;

	private PilotBarkDirector Create(
		double globalGap = 5, double pilotCooldown = 10, Random? rng = null,
		Func<string, PilotPersonality>? personalityReader = null)
	{
		var director = new PilotBarkDirector(
			rng ?? new Random(1), globalGap, pilotCooldown, personalityReader);
		foreach (var pilotId in new[] { "a", "b" })
		{
			director.RegisterPilot(pilotId, line =>
			{
				if (!_canShow)
					return false;

				_said.Add((pilotId, line));
				return true;
			});
		}
		return director;
	}

	[Theory]
	[InlineData(-5, EBarkFlavor.Anxious)]
	[InlineData(5, EBarkFlavor.Bloodthirsty)]
	public void Select_ExcludesOpposingFlavor(int threatResponse, EBarkFlavor expected)
	{
		using var director = Create(personalityReader: _ => new(threatResponse, 0, 0));

		var decision = Assert.Single(director.Select(
			["a"], [EBarkFlavor.Anxious, EBarkFlavor.Bloodthirsty], 1));

		Assert.Equal(expected, decision.Flavor);
	}

	[Fact]
	public void Select_WithOnlyOpposingFlavors_DoesNotRollLineOrStartCooldown()
	{
		var rng = new RecordingRandom(0);
		using var director = Create(rng: rng, personalityReader: _ => new(5, 0, 0));

		Assert.Empty(director.Select(["a"], [EBarkFlavor.Anxious], 1));
		Assert.Equal(new[] { "pick:1", "pick:1", "chance" }, rng.Calls);
		Assert.True(director.Offer("a", EBarkFlavor.Bloodthirsty, 1));
		Assert.True(director.Script("a", "Scripted dialogue remains unrestricted."));
	}

	[Fact]
	public void Select_UsesEachPilotsCurrentPersonality()
	{
		var personalities = new Dictionary<string, PilotPersonality>
		{
			["a"] = new(-5, 0, 0),
			["b"] = new(5, 0, 0),
		};
		using var director = Create(personalityReader: id => personalities[id]);
		var flavors = new[] { EBarkFlavor.Anxious, EBarkFlavor.Bloodthirsty };

		var decisions = director.Select(["a", "b"], flavors, 1, 2, 2);

		Assert.Equal(EBarkFlavor.Anxious, decisions.Single(d => d.PilotId == "a").Flavor);
		Assert.Equal(EBarkFlavor.Bloodthirsty, decisions.Single(d => d.PilotId == "b").Flavor);
		personalities["a"] = new(5, 0, 0);
		Assert.Equal(EBarkFlavor.Bloodthirsty,
			Assert.Single(director.Select(["a"], flavors, 1)).Flavor);
	}

	[Fact]
	public void SelectPlayerHit_UsesPersonalityAndPresentDoesNotReroll()
	{
		var personality = new PilotPersonality(5, -5, 5);
		using var director = Create(rng: new RecordingRandom(0), personalityReader: _ => personality);
		var impact = new ImpactFacts("enemy", "ship-a", EImpactCause.HighSpeedRod,
			default, 0, 10, 0, 10);
		var decision = Assert.Single(director.SelectPlayerHit(
			impact, ETeam.Player, ["a"]));

		Assert.Equal(EBarkFlavor.Bloodthirsty, decision.Flavor);
		personality = new(-5, 5, -5);
		Assert.True(director.Present(decision));
		Assert.Equal(decision.Text, Assert.Single(_said).Line);
	}

	[Fact]
	public void RunBoundDirector_UsesEffectivePersonalityAndRejectsMissingPilots()
	{
		using var run = RunTestKit.CreateFighterSupportRun();
		var pilot = Pilot.FromRecruitProfile(null,
			new PilotRecruitProfile("Psycho", "pilot_01", EPilotBackgroundId.GearPsycho));
		run.PilotRegistry.Register(pilot);
		using var director = new PilotBarkDirector(run, new RecordingRandom(0));
		director.RegisterPilot(pilot.Id, _ => true);
		var flavors = new[] { EBarkFlavor.Anxious, EBarkFlavor.Bloodthirsty };

		Assert.Equal(EBarkFlavor.Bloodthirsty,
			Assert.Single(director.Select([pilot.Id], flavors, 1)).Flavor);
		run.PilotRegistry.Update(pilot.WithPersonality(new(-5, 0, 0)));
		Assert.Equal(EBarkFlavor.Anxious,
			Assert.Single(director.Select([pilot.Id], flavors, 1)).Flavor);
		director.RegisterPilot("missing", _ => true);
		Assert.Throws<KeyNotFoundException>(() => director.Select(["missing"], flavors, 1));
	}

	[Fact]
	public void SelectPlayerHit_SelectsLivingCandidatesWithoutSpeaking()
	{
		var director = Create(rng: new RecordingRandom(0));
		var impact = new ImpactFacts("enemy", "ship-a", EImpactCause.HighSpeedRod,
			default, 1, 0, 10, 10);

		var decisions = director.SelectPlayerHit(impact, ETeam.Player, ["a", "b"]);

		Assert.Equal(2, decisions.Count);
		Assert.All(decisions, decision => Assert.Equal(EBarkFlavor.Entitled, decision.Flavor));
		Assert.Empty(_said);
	}

	[Theory]
	[InlineData(ETeam.Enemy, 1)]
	[InlineData(ETeam.Player, 0)]
	public void SelectPlayerHit_IgnoresEnemiesAndZeroDamage(ETeam team, int damage)
	{
		var rng = new RecordingRandom(0);
		var director = Create(rng: rng);
		var impact = new ImpactFacts("source", "target", EImpactCause.RamImpact,
			default, 0, damage, 0, 10);

		Assert.Empty(director.SelectPlayerHit(impact, team, ["a", "b"]));
		Assert.Empty(rng.Calls);
	}

	[Fact]
	public void SelectPlayerHit_OnlyUsesProvidedLivingCandidates()
	{
		var director = Create(rng: new RecordingRandom(0));
		var impact = new ImpactFacts("enemy", "ship-a", EImpactCause.HighSpeedRod,
			default, 0, 10, 0, 10);

		var decision = Assert.Single(director.SelectPlayerHit(impact, ETeam.Player, ["b"]));

		Assert.Equal("b", decision.PilotId);
	}

	[Theory]
	[InlineData(0.199, true)]
	[InlineData(0.2, false)]
	[InlineData(0.3, false)]
	public void SelectPlayerHit_UsesTwentyPercentChance(double roll, bool shouldSpeak)
	{
		var director = Create(rng: new RecordingRandom(roll));
		var impact = new ImpactFacts("enemy", "ship-a", EImpactCause.HighSpeedRod,
			default, 1, 0, 10, 10);

		var decisions = director.SelectPlayerHit(impact, ETeam.Player, ["a"]);

		Assert.Equal(shouldSpeak ? 1 : 0, decisions.Count);
		Assert.Empty(_said);
	}

	[Fact]
	public void SelectPlayerHit_RollsChanceIndependentlyWithoutReplacingSkippedPilot()
	{
		var rng = new RecordingRandom(0.2, 0.199);
		var director = Create(rng: rng);
		var impact = new ImpactFacts("enemy", "ship-a", EImpactCause.HighSpeedRod,
			default, 1, 0, 10, 10);

		var decision = Assert.Single(director.SelectPlayerHit(impact, ETeam.Player, ["a", "b"]));

		Assert.Equal("a", decision.PilotId);
		Assert.Equal(2, rng.Calls.Count(call => call == "chance"));
	}

	[Theory]
	[InlineData(0)]
	[InlineData(5)]
	public void SelectPlayerHit_IgnoresCooldownsAndCanRepeatWithoutTimeAdvancing(double elapsed)
	{
		var director = Create(rng: new RecordingRandom(0));
		Assert.True(director.Script("a", "Already spoke."));
		director.Tick(elapsed);
		var impact = new ImpactFacts("enemy", "ship-a", EImpactCause.HighSpeedRod,
			default, 1, 0, 10, 10);

		for (var i = 0; i < 2; i++)
		{
			var decision = Assert.Single(director.SelectPlayerHit(impact, ETeam.Player, ["a"]));
			Assert.True(director.Present(decision));
		}

		Assert.Equal(3, _said.Count);
	}

	[Theory]
	[InlineData(5, 0)]
	[InlineData(0, 10)]
	public void Present_PlayerHit_DoesNotStartStrategicCooldowns(double globalGap, double pilotCooldown)
	{
		var director = Create(globalGap, pilotCooldown, new RecordingRandom(0));
		var impact = new ImpactFacts("enemy", "ship-a", EImpactCause.HighSpeedRod,
			default, 1, 0, 10, 10);
		var decision = Assert.Single(director.SelectPlayerHit(impact, ETeam.Player, ["a"]));

		Assert.True(director.Present(decision));

		Assert.True(director.Offer("a", EBarkFlavor.Impatient, 1));
	}

	[Fact]
	public void Present_PlayerHit_DoesNotRefreshExistingStrategicCooldowns()
	{
		var director = Create(rng: new RecordingRandom(0));
		Assert.True(director.Script("a", "Already spoke."));
		director.Tick(9);
		var impact = new ImpactFacts("enemy", "ship-a", EImpactCause.HighSpeedRod,
			default, 1, 0, 10, 10);
		var decision = Assert.Single(director.SelectPlayerHit(impact, ETeam.Player, ["a"]));

		Assert.True(director.Present(decision));
		Assert.False(director.Offer("a", EBarkFlavor.Impatient, 1));
		director.Tick(1);

		Assert.True(director.Offer("a", EBarkFlavor.Impatient, 1));
	}

	[Fact]
	public void Select_DoesNotSpeakOrStartCooldowns()
	{
		var director = Create();

		var decision = Assert.Single(director.Select(["a"], [EBarkFlavor.Bloodthirsty], 1));

		Assert.Empty(_said);
		Assert.Equal("a", decision.PilotId);
		Assert.Equal(EBarkFlavor.Bloodthirsty, decision.Flavor);
		Assert.Contains(decision.Text, PilotBarkCatalog.LinesFor(EBarkFlavor.Bloodthirsty));
		Assert.Single(director.Select(["a"], [EBarkFlavor.Bloodthirsty], 1));
		Assert.True(director.Present(decision));
		Assert.Equal(decision.Text, Assert.Single(_said).Line);
		Assert.Empty(director.Select(["b"], [EBarkFlavor.Bloodthirsty], 1));
	}

	[Fact]
	public void Present_DoesNotRerollOrBlockOtherSelectedSpeakers()
	{
		var rng = new RecordingRandom(0);
		var director = Create(rng: rng);
		var decisions = director.Select(["a", "b"], [EBarkFlavor.Anxious], 1, 2, 2);
		var rollCount = rng.Calls.Count;

		foreach (var decision in decisions)
			Assert.True(director.Present(decision));

		Assert.Equal(2, _said.Count);
		Assert.Equal(rollCount, rng.Calls.Count);
	}

	[Fact]
	public void Present_WhenHandleRemoved_DoesNotStartCooldowns()
	{
		var director = Create();
		var decision = Assert.Single(director.Select(["a"], [EBarkFlavor.Bloodthirsty], 1));
		director.UnregisterPilot("a");

		Assert.False(director.Present(decision));
		Assert.Empty(_said);
		Assert.True(director.Offer("b", EBarkFlavor.Bloodthirsty, 1));
	}

	[Fact]
	public void Offer_WithFullChance_SaysLineFromFlavorPool()
	{
		var director = Create();

		Assert.True(director.Offer("a", EBarkFlavor.Bloodthirsty, 1));

		var (pilotId, line) = Assert.Single(_said);
		Assert.Equal("a", pilotId);
		Assert.Contains(line, PilotBarkCatalog.LinesFor(EBarkFlavor.Bloodthirsty));
	}

	[Fact]
	public void Offer_WithZeroChance_NeverSpeaks()
	{
		var director = Create(globalGap: 0, pilotCooldown: 0);

		for (var i = 0; i < 50; i++)
			Assert.False(director.Offer("a", EBarkFlavor.Impatient, 0));

		Assert.Empty(_said);
	}

	[Fact]
	public void Offer_WithPartialChance_SpeaksSometimes()
	{
		var director = Create(globalGap: 0, pilotCooldown: 0);

		var spoke = Enumerable.Range(0, 200).Count(_ => director.Offer("a", EBarkFlavor.Impatient, 0.5));

		Assert.InRange(spoke, 60, 140);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void Offer_WithoutChance_UsesDefaultChance(bool shouldSpeak)
	{
		var roll = shouldSpeak ? PilotBarkDirector.DefaultChance / 2 : PilotBarkDirector.DefaultChance;
		var director = Create(rng: new RecordingRandom(roll));

		Assert.Equal(shouldSpeak, director.Offer("a", EBarkFlavor.Impatient));
		Assert.Equal(shouldSpeak ? 1 : 0, _said.Count);
	}

	[Fact]
	public void Offer_BlockedByGlobalGap_ForAnyPilot()
	{
		var director = Create(globalGap: 5, pilotCooldown: 0);
		director.Offer("a", EBarkFlavor.Entitled, 1);

		director.Tick(4.9);
		Assert.False(director.Offer("b", EBarkFlavor.Entitled, 1));

		director.Tick(0.2);
		Assert.True(director.Offer("b", EBarkFlavor.Entitled, 1));
	}

	[Fact]
	public void Offer_BlockedByPilotCooldown_OnlyForThatPilot()
	{
		var director = Create(globalGap: 0, pilotCooldown: 10);
		director.Offer("a", EBarkFlavor.Entitled, 1);

		director.Tick(5);
		Assert.False(director.Offer("a", EBarkFlavor.Entitled, 1));
		Assert.True(director.Offer("b", EBarkFlavor.Entitled, 1));

		director.Tick(5);
		Assert.True(director.Offer("a", EBarkFlavor.Entitled, 1));
	}

	[Fact]
	public void Offer_WhenLineCannotBeShown_DoesNotStartCooldown()
	{
		var director = Create();
		_canShow = false;
		Assert.False(director.Offer("a", EBarkFlavor.Anxious, 1));

		_canShow = true;
		Assert.True(director.Offer("a", EBarkFlavor.Anxious, 1));
	}

	[Fact]
	public void Offer_WithCandidates_RollsCountPilotChanceFlavorAndLine()
	{
		var rng = new RecordingRandom(0.69);
		var director = Create(rng: rng);

		Assert.True(director.Offer(["a", "b"], [EBarkFlavor.Entitled, EBarkFlavor.Supportive], 0.7));

		Assert.Equal("b", Assert.Single(_said).PilotId);
		Assert.Contains(_said[0].Line, PilotBarkCatalog.LinesFor(EBarkFlavor.Supportive));
		Assert.Equal(new[] { "pick:1", "pick:2", "chance", "pick:2",
			$"pick:{PilotBarkCatalog.LinesFor(EBarkFlavor.Supportive).Count}" }, rng.Calls);
	}

	[Fact]
	public void Offer_Group_CapsCountAndSelectsDistinctPilotsWithoutBlockingGroup()
	{
		var rng = new RecordingRandom(0);
		var director = Create(rng: rng);

		Assert.True(director.Offer(["a", "a", "b"], [EBarkFlavor.Bloodthirsty, EBarkFlavor.Impatient],
			minPilots: 1, maxPilots: 3));

		Assert.Equal(2, _said.Count);
		Assert.Equal(2, _said.Select(line => line.PilotId).Distinct().Count());
		Assert.Equal(2, rng.Calls.Count(call => call == "chance"));
		Assert.Equal(new[] { "pick:2", "pick:2", "chance", "pick:2",
			$"pick:{PilotBarkCatalog.LinesFor(EBarkFlavor.Impatient).Count}",
			"pick:1", "chance", "pick:2", $"pick:{PilotBarkCatalog.LinesFor(EBarkFlavor.Impatient).Count}" }, rng.Calls);
		Assert.False(director.Offer(["a", "b"], [EBarkFlavor.Bloodthirsty], 1, 1, 3));
	}

	[Fact]
	public void Offer_Group_WithOneEligiblePilot_SpeaksOnce()
	{
		var director = Create(rng: new RecordingRandom(0));
		director.UnregisterPilot("b");

		Assert.True(director.Offer(["a", "b"], [EBarkFlavor.Anxious], 1, 1, 3));

		Assert.Equal("a", Assert.Single(_said).PilotId);
	}

	[Theory]
	[InlineData(0, 1)]
	[InlineData(2, 1)]
	public void Offer_Group_RejectsInvalidCountRange(int min, int max)
	{
		var director = Create();

		Assert.Throws<ArgumentOutOfRangeException>(() =>
			director.Offer(["a"], [EBarkFlavor.Anxious], minPilots: min, maxPilots: max));
	}

	[Fact]
	public void Offer_WithCandidates_WhenChanceMisses_DoesNotRollFlavorOrReplacePilot()
	{
		var rng = new RecordingRandom(0.7);
		var director = Create(rng: rng);

		Assert.False(director.Offer(["a", "b"], [EBarkFlavor.Entitled, EBarkFlavor.Supportive], 0.7));

		Assert.Equal(new[] { "pick:1", "pick:2", "chance" }, rng.Calls);
		Assert.Empty(_said);
	}

	[Fact]
	public void Offer_WithCandidates_ExcludesPilotsOnCooldown()
	{
		var director = Create(globalGap: 0, pilotCooldown: 10);
		director.Script("a", "Already spoke.");

		Assert.True(director.Offer(["a", "b"], [EBarkFlavor.Supportive], 1));

		Assert.Equal("b", _said[^1].PilotId);
		Assert.False(director.Offer(["a", "b"], [EBarkFlavor.Supportive], 1));
	}

	[Fact]
	public void Offer_WithoutCandidates_DoesNotRoll()
	{
		var rng = new RecordingRandom(0);
		var director = Create(rng: rng);

		Assert.False(director.Offer([], [EBarkFlavor.Supportive]));

		Assert.Empty(rng.Calls);
	}

	[Fact]
	public void Offer_WithoutFlavors_Throws()
	{
		var director = Create();

		Assert.Throws<ArgumentException>(() => director.Offer(["a"], []));
	}

	[Fact]
	public void Script_BypassesCooldownsAndStartsThem()
	{
		var director = Create();
		director.Offer("a", EBarkFlavor.Entitled, 1);

		Assert.True(director.Script("a", "Scripted."));
		Assert.Equal("Scripted.", _said[^1].Line);

		director.Tick(4);
		Assert.False(director.Offer("b", EBarkFlavor.Entitled, 1));
	}

	[Fact]
	public void Offer_Group_RollsChanceIndependentlyAndDoesNotCooldownSkippedPilot()
	{
		var rng = new RecordingRandom(0.7, 0.69);
		var director = Create(globalGap: 0, rng: rng);

		Assert.True(director.Offer(["a", "b"], [EBarkFlavor.Supportive], 0.7, minPilots: 2, maxPilots: 2));

		Assert.Equal("a", Assert.Single(_said).PilotId);
		Assert.Equal(2, rng.Calls.Count(call => call == "chance"));
		Assert.True(director.Offer("b", EBarkFlavor.Supportive, 1));
	}

	private sealed class RecordingRandom(double chance, double? subsequentChance = null) : Random
	{
		public List<string> Calls { get; } = [];
		private int _chanceCalls;

		public override double NextDouble()
		{
			Calls.Add("chance");
			return _chanceCalls++ == 0 ? chance : subsequentChance ?? chance;
		}

		public override int Next(int maxValue)
		{
			Calls.Add($"pick:{maxValue}");
			return maxValue - 1;
		}
	}
}

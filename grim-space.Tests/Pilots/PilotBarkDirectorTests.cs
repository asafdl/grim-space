using GrimSpace.Pilots.Barks;

namespace GrimSpace.Tests.Pilots;

[IntegrationTestSuite]
public sealed class PilotBarkDirectorTests
{
	private readonly List<(string PilotId, string Line)> _said = [];
	private bool _canShow = true;

	private PilotBarkDirector Create(double globalGap = 5, double pilotCooldown = 10, Random? rng = null)
	{
		var director = new PilotBarkDirector(rng ?? new Random(1), globalGap, pilotCooldown);
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

	[Fact]
	public void Offer_WithFullChance_SaysLineFromFlavorPool()
	{
		var director = Create();

		Assert.True(director.Offer("a", EBarkFlavor.Angry, 1));

		var (pilotId, line) = Assert.Single(_said);
		Assert.Equal("a", pilotId);
		Assert.Contains(line, PilotBarkCatalog.LinesFor(EBarkFlavor.Angry));
	}

	[Fact]
	public void Offer_WithZeroChance_NeverSpeaks()
	{
		var director = Create(globalGap: 0, pilotCooldown: 0);

		for (var i = 0; i < 50; i++)
			Assert.False(director.Offer("a", EBarkFlavor.Bored, 0));

		Assert.Empty(_said);
	}

	[Fact]
	public void Offer_WithPartialChance_SpeaksSometimes()
	{
		var director = Create(globalGap: 0, pilotCooldown: 0);

		var spoke = Enumerable.Range(0, 200).Count(_ => director.Offer("a", EBarkFlavor.Bored, 0.5));

		Assert.InRange(spoke, 60, 140);
	}

	[Fact]
	public void Offer_WithoutChance_UsesDefaultChance()
	{
		var director = Create(globalGap: 0, pilotCooldown: 0);

		var spoke = Enumerable.Range(0, 200).Count(_ => director.Offer("a", EBarkFlavor.Bored));

		Assert.InRange(spoke, 110, 170);
	}

	[Fact]
	public void Offer_BlockedByGlobalGap_ForAnyPilot()
	{
		var director = Create(globalGap: 5, pilotCooldown: 0);
		director.Offer("a", EBarkFlavor.Smug, 1);

		director.Tick(4.9);
		Assert.False(director.Offer("b", EBarkFlavor.Smug, 1));

		director.Tick(0.2);
		Assert.True(director.Offer("b", EBarkFlavor.Smug, 1));
	}

	[Fact]
	public void Offer_BlockedByPilotCooldown_OnlyForThatPilot()
	{
		var director = Create(globalGap: 0, pilotCooldown: 10);
		director.Offer("a", EBarkFlavor.Smug, 1);

		director.Tick(5);
		Assert.False(director.Offer("a", EBarkFlavor.Smug, 1));
		Assert.True(director.Offer("b", EBarkFlavor.Smug, 1));

		director.Tick(5);
		Assert.True(director.Offer("a", EBarkFlavor.Smug, 1));
	}

	[Fact]
	public void Offer_WhenLineCannotBeShown_DoesNotStartCooldown()
	{
		var director = Create();
		_canShow = false;
		Assert.False(director.Offer("a", EBarkFlavor.Scared, 1));

		_canShow = true;
		Assert.True(director.Offer("a", EBarkFlavor.Scared, 1));
	}

	[Fact]
	public void Offer_WithCandidates_RollsChanceThenPilotFlavorAndLine()
	{
		var rng = new RecordingRandom(0.69);
		var director = Create(rng: rng);

		Assert.True(director.Offer(["a", "b"], [EBarkFlavor.Disdain, EBarkFlavor.Relieved]));

		Assert.Equal("b", Assert.Single(_said).PilotId);
		Assert.Contains(_said[0].Line, PilotBarkCatalog.LinesFor(EBarkFlavor.Relieved));
		Assert.Equal(new[] { "chance", "pick:2", "pick:2", "pick:1" }, rng.Calls);
	}

	[Fact]
	public void Offer_WithCandidates_WhenChanceMisses_DoesNotRollForEachPilot()
	{
		var rng = new RecordingRandom(0.7);
		var director = Create(rng: rng);

		Assert.False(director.Offer(["a", "b"], [EBarkFlavor.Disdain, EBarkFlavor.Relieved]));

		Assert.Equal(new[] { "chance" }, rng.Calls);
		Assert.Empty(_said);
	}

	[Fact]
	public void Offer_WithCandidates_ExcludesPilotsOnCooldown()
	{
		var director = Create(globalGap: 0, pilotCooldown: 10);
		director.Script("a", "Already spoke.");

		Assert.True(director.Offer(["a", "b"], [EBarkFlavor.Relieved], 1));

		Assert.Equal("b", _said[^1].PilotId);
		Assert.False(director.Offer(["a", "b"], [EBarkFlavor.Relieved], 1));
	}

	[Fact]
	public void Offer_WithoutCandidates_DoesNotRoll()
	{
		var rng = new RecordingRandom(0);
		var director = Create(rng: rng);

		Assert.False(director.Offer([], [EBarkFlavor.Relieved]));

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
		director.Offer("a", EBarkFlavor.Smug, 1);

		Assert.True(director.Script("a", "Scripted."));
		Assert.Equal("Scripted.", _said[^1].Line);

		director.Tick(4);
		Assert.False(director.Offer("b", EBarkFlavor.Smug, 1));
	}

	private sealed class RecordingRandom(double chance) : Random
	{
		public List<string> Calls { get; } = [];

		public override double NextDouble()
		{
			Calls.Add("chance");
			return chance;
		}

		public override int Next(int maxValue)
		{
			Calls.Add($"pick:{maxValue}");
			return maxValue - 1;
		}
	}
}

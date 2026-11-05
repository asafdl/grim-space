using GrimSpace.Pilots.Barks;
using GrimSpace.Core.Log;
using GrimSpace.Run;
using GrimSpace.Tests.World.StarSystem.Contracts;
using GrimSpace.Tutorials;
using GrimSpace.World.StarSystem.Actions;

namespace GrimSpace.Tests.Pilots;

[IntegrationTestSuite]
public sealed class PilotBarkEventTests
{
	[Fact]
	public void RunDirector_KeepsCooldownsWhenSceneHandlesChange()
	{
		using var run = State.CreateNewRun(42);
		run.TryConsumeOpening();
		var director = run.Barks;
		var pilotId = run.PilotRegistry.Active[0].Id;
		director.RegisterPilot(pilotId, _ => true);
		Assert.True(director.Offer(pilotId, EBarkFlavor.Relieved, 1));
		director.UnregisterPilot(pilotId);
		director.Tick(1);
		director.RegisterPilot(pilotId, _ => true);

		Assert.Same(director, run.Barks);
		Assert.False(director.Offer(pilotId, EBarkFlavor.Relieved, 1));
		director.Tick(PilotBarkDirector.DefaultPilotCooldownSeconds);
		Assert.True(director.Offer(pilotId, EBarkFlavor.Relieved, 1));
	}

	[Fact]
	public void RunDirector_DoesNotConsumeOpeningBeforeHandlesRegister()
	{
		using var run = State.CreateNewRun(42);
		run.Barks.Tick(1);
		var lines = new List<string>();
		run.Barks.RegisterPilot(run.PilotRegistry.Active[0].Id, line =>
		{
			lines.Add(line);
			return true;
		});

		run.Barks.Tick(1);

		Assert.Equal(PilotBarkCatalog.OpeningLines, lines);
	}

	[Fact]
	public void RunDirector_RebindsAfterMapRegeneration()
	{
		using var run = State.CreateNewRun(42);
		var director = run.Barks;

		run.RegenerateMap(43);
		var logs = new List<string>();
		using var scope = GameLog.BeginScope(logs.Add);
		CompleteDelivery(run);

		Assert.Same(director, run.Barks);
		Assert.Single(logs, line => line == "[Barks] Ignore: no candidate pilots.");
	}

	[Fact]
	public void DeliveryCompletion_SpeaksThroughRegisteredHandle()
	{
		using var run = State.CreateNewRun(42);
		run.TryConsumeOpening();
		using var director = new PilotBarkDirector(run, new AlwaysSpeakRandom());
		var lines = new List<string>();
		director.RegisterPilot(run.PilotRegistry.Active[0].Id, line =>
		{
			lines.Add(line);
			return true;
		});

		CompleteDelivery(run);

		Assert.Contains(Assert.Single(lines),
			PilotBarkCatalog.LinesFor(EBarkFlavor.Disdain));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void RemovedHandleOrDisposedDirector_DoesNotSpeak(bool dispose)
	{
		using var run = State.CreateNewRun(42);
		run.TryConsumeOpening();
		using var director = new PilotBarkDirector(run, new AlwaysSpeakRandom());
		var pilotId = run.PilotRegistry.Active[0].Id;
		director.RegisterPilot(pilotId, _ => throw new InvalidOperationException("Stale handle called."));
		if (dispose)
			director.Dispose();
		else
			director.UnregisterPilot(pilotId);

		CompleteDelivery(run);
	}

	[Fact]
	public void AcceptingDelivery_DoesNotSpeak()
	{
		using var run = State.CreateNewRun(42);
		run.TryConsumeOpening();
		using var director = new PilotBarkDirector(run, new AlwaysSpeakRandom());
		director.RegisterPilot(run.PilotRegistry.Active[0].Id,
			_ => throw new InvalidOperationException("Acceptance must not trigger a bark."));

		var contractId = TutorialBeatContracts.OfferBeatB(run.StarSystem.Map);
		run.StarSystem.CommitSetup(new AcceptContractAction(State.PlayerFleetUnitId, contractId));

		Assert.False(run.StarSystem.Map.ContractRegistry.IsCompleted(contractId));
	}

	[Fact]
	public void UnregisteredPilot_CanRegisterANewHandle()
	{
		using var run = State.CreateNewRun(42);
		run.TryConsumeOpening();
		using var director = new PilotBarkDirector(run, new AlwaysSpeakRandom());
		var pilotId = run.PilotRegistry.Active[0].Id;
		director.RegisterPilot(pilotId, _ => throw new InvalidOperationException("Old handle called."));
		director.UnregisterPilot(pilotId);
		var spoken = false;
		director.RegisterPilot(pilotId, _ => spoken = true);

		CompleteDelivery(run);

		Assert.True(spoken);
	}

	[Fact]
	public void RegisteredHandle_ForNonActivePilot_IsNotSelected()
	{
		using var run = State.CreateNewRun(42);
		run.TryConsumeOpening();
		using var director = new PilotBarkDirector(run, new AlwaysSpeakRandom());
		director.RegisterPilot("not-an-active-pilot",
			_ => throw new InvalidOperationException("Inactive pilot selected."));

		CompleteDelivery(run);
	}

	[Fact]
	public void Tick_PlaysOpeningOnceThroughRegisteredHandle()
	{
		using var run = State.CreateNewRun(42);
		using var director = new PilotBarkDirector(run, new AlwaysSpeakRandom());
		var lines = new List<string>();
		director.RegisterPilot(run.PilotRegistry.Active[0].Id, line =>
		{
			lines.Add(line);
			return true;
		});

		director.Tick(0);
		director.Tick(1);

		Assert.Equal(PilotBarkCatalog.OpeningLines, lines);
	}

	private static void CompleteDelivery(State run)
	{
		var map = run.StarSystem.Map;
		var contractId = TutorialBeatContracts.OfferBeatB(map);
		run.StarSystem.CommitSetup(
			new AcceptContractAction(State.PlayerFleetUnitId, contractId));
		run.StarSystem.CommitSetup(
			ContractActionTestContext.TurnInDelivery(map, State.PlayerFleetUnitId, contractId));
		Assert.True(map.ContractRegistry.IsCompleted(contractId));
	}

	private sealed class AlwaysSpeakRandom : Random
	{
		public override double NextDouble() => 0;
		public override int Next(int maxValue) => 0;
	}
}

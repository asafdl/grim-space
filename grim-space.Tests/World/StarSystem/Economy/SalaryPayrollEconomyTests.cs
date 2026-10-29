using GrimSpace.Tests.Run;
using GrimSpace.World.StarSystem.Economy;
using GrimSpace.World.StarSystem.Resources;
using RunState = GrimSpace.Run.State;

namespace GrimSpace.Tests.World.StarSystem.Economy;

[StarSystemTestSuite]
public sealed class SalaryPayrollEconomyTests
{
	[Fact]
	public void Cadence_PaysRosterAndClearsMissedSalary()
	{
		using var run = RunState.CreateNewRun(42);
		var pilot = RunTestKit.StarterRangerPilot(run);
		run.PilotRegistry.Update(pilot.WithSalaryPayroll(2));
		Assert.True(
			run.StarSystem.Map.PlayerResources.TryApply(
				ResourceBundle.Of(ResourceId.Credits, 100)));

		var due = run.PilotRegistry.Active.Sum(active =>
			StrategicEconomyConfig.SalaryFor(active.BaseSalaryCredits, active.ClaimedLevel));
		var creditsBefore = run.StarSystem.Map.PlayerResources.GetBalance(ResourceId.Credits);

		AdvanceToSalaryCadence(run);

		Assert.Equal(
			creditsBefore - due,
			run.StarSystem.Map.PlayerResources.GetBalance(ResourceId.Credits));
		Assert.True(run.PilotRegistry.TryGet(pilot.Id, out var after));
		Assert.Equal(0, after.MissedSalaryPeriods);
	}

	[Fact]
	public void Cadence_IncrementsMissWhenCreditsShort()
	{
		using var run = RunState.CreateNewRun(42);
		var pilot = RunTestKit.StarterRangerPilot(run);
		var credits = run.StarSystem.Map.PlayerResources.GetBalance(ResourceId.Credits);
		Assert.True(
			run.StarSystem.Map.PlayerResources.TryApply(
				ResourceBundle.Of(ResourceId.Credits, -credits)));
		Assert.Equal(0, run.StarSystem.Map.PlayerResources.GetBalance(ResourceId.Credits));

		AdvanceToSalaryCadence(run);

		Assert.True(run.PilotRegistry.TryGet(pilot.Id, out var after));
		Assert.Equal(1, after.MissedSalaryPeriods);
	}

	[Fact]
	public void SaveLoad_PreservesSalaryFields()
	{
		using var run = RunState.CreateNewRun(42);
		var pilot = RunTestKit.StarterRangerPilot(run);
		run.PilotRegistry.Update(pilot.WithSalaryPayroll(3));

		var registry = GrimSpace.Run.Persistence.PersistenceRegistry.CreateDefault();
		var snapshot = run.CaptureSnapshot(registry);
		using var restored = RunState.FromSnapshot(snapshot, registry);

		Assert.True(restored.PilotRegistry.TryGet(pilot.Id, out var after));
		Assert.Equal(pilot.BaseSalaryCredits, after.BaseSalaryCredits);
		Assert.Equal(3, after.MissedSalaryPeriods);
		Assert.Equal(0, after.MissedHazardPeriods);
	}

	[Fact]
	public void StarterPilot_HasDefaultBaseSalary()
	{
		using var run = RunState.CreateNewRun(42);
		var pilot = RunTestKit.StarterRangerPilot(run);
		Assert.Equal(StrategicEconomyConfig.DefaultBaseSalaryCredits, pilot.BaseSalaryCredits);
	}

	private static void AdvanceToSalaryCadence(RunState run)
	{
		var ticks = StrategicEconomyConfig.SalaryCadenceTicks - run.StarSystem.Tick;
		while (ticks <= 0)
			ticks += StrategicEconomyConfig.SalaryCadenceTicks;
		run.StarSystem.AdvanceTicks(ticks);
		Assert.Equal(0, run.StarSystem.Tick % StrategicEconomyConfig.SalaryCadenceTicks);
	}
}

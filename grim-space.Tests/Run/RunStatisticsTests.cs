using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Objectives;
using GrimSpace.Core.Actions;
using GrimSpace.Run;
using GrimSpace.Run.Persistence;
using GrimSpace.Tests.World.StarSystem.Traffic;
using GrimSpace.Units;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Encounter;
using GrimSpace.World.StarSystem.Resources;
using RunState = GrimSpace.Run.State;

namespace GrimSpace.Tests.Run;

[IntegrationTestSuite]
public sealed class RunStatisticsTests
{
	[Fact]
	public void RecordBattle_TracksWinsAndLosses()
	{
		var stats = new RunStatistics();
		stats.RecordBattle(EBattleResult.Win);
		stats.RecordBattle(EBattleResult.Win);
		stats.RecordBattle(EBattleResult.Lose);
		stats.RecordBattle(EBattleResult.Tie);

		Assert.Equal(2, stats.BattlesWon);
		Assert.Equal(1, stats.BattlesLost);
	}

	[Fact]
	public void RecordTransaction_SumsPositiveCreditsExcludingDebug()
	{
		var stats = new RunStatistics();
		stats.RecordTransaction(new Transaction(
			TransactionSource.ContractPayment,
			ResourceBundle.Of(ResourceId.Credits, 100)));
		stats.RecordTransaction(new Transaction(
			TransactionSource.BattleLoot,
			ResourceBundle.Of(ResourceId.Credits, 40)));
		stats.RecordTransaction(new Transaction(
			TransactionSource.MerchantPurchase,
			ResourceBundle.Of(ResourceId.Credits, -25)));
		stats.RecordTransaction(new Transaction(
			TransactionSource.DebugGrant,
			ResourceBundle.Of(ResourceId.Credits, 999)));

		Assert.Equal(140, stats.CreditsEarned);
	}

	[Fact]
	public void CaptureStatisticsSummary_IncludesContractsAndBalances()
	{
		using var run = RunState.CreateNewRun(42);
		run.Statistics.RecordBattle(EBattleResult.Win);
		run.Statistics.RecordTransaction(new Transaction(
			TransactionSource.ContractPayment,
			ResourceBundle.Of(ResourceId.Credits, 75)));

		var summary = run.CaptureStatisticsSummary();

		Assert.Equal(0, summary.ContractsCompleted);
		Assert.Equal(1, summary.BattlesWon);
		Assert.Equal(0, summary.BattlesLost);
		Assert.Equal(75, summary.CreditsEarned);
		Assert.Equal(
			run.StarSystem.Map.PlayerResources.GetBalance(ResourceId.Credits),
			summary.CreditsRemaining);
	}

	[Fact]
	public void Snapshot_RoundTripsStatistics()
	{
		using var run = RunState.CreateNewRun(42);
		run.Statistics.RecordBattle(EBattleResult.Lose);
		run.Statistics.RecordTransaction(new Transaction(
			TransactionSource.WreckageSalvage,
			ResourceBundle.Of(ResourceId.Credits, 33)));

		var registry = PersistenceRegistry.CreateDefault();
		var snapshot = run.CaptureSnapshot(registry);
		using var restored = RunState.FromSnapshot(snapshot, registry);

		Assert.Equal(0, restored.Statistics.BattlesWon);
		Assert.Equal(1, restored.Statistics.BattlesLost);
		Assert.Equal(33, restored.Statistics.CreditsEarned);
	}

	[Fact]
	public void BattleOutcome_WinIncrementsBattlesWon()
	{
		using var run = RunState.CreateNewRun(42);
		var engagementId = LaunchPirateEngagement(run);
		var handoffs = run.GetFieldedShipIds()
			.Select(id => OutcomeTestKit.Handoff(
				id,
				run.ItemInventory.Get(id).Spec.Chassis,
				1))
			.ToArray();
		var outcome = new BattleOutcome(engagementId, EBattleResult.Win, handoffs);

		run.OnCommittedBattleOutcome(new Record<BattleOutcome>(outcome));

		Assert.Equal(1, run.Statistics.BattlesWon);
		Assert.Equal(0, run.Statistics.BattlesLost);
	}

	private static string LaunchPirateEngagement(RunState run)
	{
		var pirateFleet = StarSystemTestHarness.CreatePirateFleet(
			"pirate-a",
			new GrimSpace.Math.Grid.Coord(4, 0, 0),
			GrimSpace.World.Factions.EFaction.Pirates);
		run.StarSystem.Map.FleetRegistry.Add(pirateFleet);
		foreach (var declaration in pirateFleet.Registrations)
			run.ItemInventory.Register(declaration, pirateFleet.State.Id);
		new CommitEngagementEffect(RunState.PlayerFleetUnitId, pirateFleet.State.Id)
			.Apply(
				run.StarSystem.Map,
				new GrimSpace.World.StarSystem.Runtime.ActorRuntime(),
				RunState.PlayerFleetUnitId);
		var engagementId = run.StarSystem.Map.StateOf(RunState.PlayerFleetUnitId).CurrentEngagement!.Id;
		var playerFleet = run.StarSystem.Map.FleetRegistry.FleetOf(RunState.PlayerFleetUnitId);
		run.ActiveBattle = EngagementBattleFactory.Create(
			[playerFleet, pirateFleet],
			run.ItemInventory,
			1,
			engagementId,
			playerFleet.State.Id);
		return engagementId;
	}
}

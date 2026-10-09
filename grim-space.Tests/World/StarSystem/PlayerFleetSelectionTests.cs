using GrimSpace.Core.Ids;
using GrimSpace.Run;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;

namespace GrimSpace.Tests.World.StarSystem;

[StarSystemTestSuite]
public sealed class PlayerFleetSelectionTests
{
	[Fact]
	public void TryCommitPlayerInput_UpdatesRuntimeSelectedMember()
	{
		using var run = State.CreateNewRun(42);
		var first = run.PlayerParty.ShipIds[0];
		var secondId = TypedIdGenerator.NextId("gunship");
		Assert.True(run.TryEnlistPlayerShip(new ShipSpawnDeclaration(secondId, EType.Gunship)));

		var runtime = run.StarSystem.RuntimeFor(State.PlayerFleetUnitId);
		Assert.Equal(first, runtime.SelectedMemberShipId);

		Assert.True(run.StarSystem.TryCommitPlayerInput(
			new SelectActiveFleetShipAction(State.PlayerFleetUnitId, secondId)));
		Assert.Equal(secondId, runtime.SelectedMemberShipId);
	}

	[Fact]
	public void EnsureSelectedMember_FallsBackWhenSelectionLeavesParty()
	{
		using var run = State.CreateNewRun(42);
		var runtime = run.StarSystem.RuntimeFor(State.PlayerFleetUnitId);
		runtime.SelectedMemberShipId = "missing-ship";

		var resolved = PlayerFleetSelection.EnsureSelectedMember(
			run.StarSystem.Map,
			runtime,
			State.PlayerFleetUnitId,
			run.PlayerParty.ShipIds);

		Assert.Equal(run.PlayerParty.ShipIds[0], resolved);
		Assert.Equal(run.PlayerParty.ShipIds[0], runtime.SelectedMemberShipId);
	}
}

using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.PilotCapabilities;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.PilotCapabilities;

public sealed class CorporateSlaveInitiativeTests
{
	[Fact]
	public void CreateBattleUnit_AppliesInitiativePenaltyOnce()
	{
		var pilotId = "pilot-a";
		var spawn = new BattleSpawn
		{
			Ship = ShipInstance.FromCatalog("fighter-a", EType.Fighter),
			Pilot = new PilotBattleSnapshot(
				pilotId,
				PilotCapabilityGrant.FromBackground(
					pilotId,
					EPilotBackgroundId.CorporateSlave,
					BattlePilotCapabilityPipelineFactory.Supports)),
			Team = ETeam.Player,
			Position = Coord.Zero,
			ExecutionAgent = new UserExecutionAgent(),
		};

		var unit = Factory.Create(spawn);
		var cloned = Factory.Create(unit.State.Clone(), ETeam.Player, EBattleAgentKind.Player);

		Assert.Equal(30, unit.State.Initiative);
		Assert.Equal(30, cloned.State.Initiative);
		Assert.Equal(40, spawn.Ship.Spec.Initiative);
	}
}

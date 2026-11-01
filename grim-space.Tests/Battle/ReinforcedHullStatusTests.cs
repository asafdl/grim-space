using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.StatusModel;

public sealed class ReinforcedHullStatusTests
{
	[Fact]
	public void RewriteHullDamage_ScalesHullOnly()
	{
		var player = BattleTestFixture.Player(Coord.Zero);
		BattlePassiveStatusInstaller.InstallFromCapabilities(
			player.State,
			ActorRuntime.From(PilotBattleSnapshot.FromSkills("pilot", [EPilotSkillId.ReinforcedHull])));

		Assert.Equal(
			67,
			BattleStatuses.RewriteHullDamage(player.State.Statuses, 100));
		Assert.Equal(
			100,
			BattleStatuses.RewriteShieldDamage(player.State.Statuses, 100));
		Assert.Equal(
			100,
			BattleStatuses.RewriteIncomingDamage(player.State.Statuses, 100));
	}

	[Fact]
	public void ApplyDamage_UsesHullChannel()
	{
		var player = BattleTestFixture.Player(Coord.Zero);
		BattlePassiveStatusInstaller.InstallFromCapabilities(
			player.State,
			ActorRuntime.From(PilotBattleSnapshot.FromSkills("pilot", [EPilotSkillId.ReinforcedHull])));
		player.State.ShieldPoints[ESpatialOrientation.Forward] = 0;
		var hullBefore = player.State.HullPoints;

		Defense.ApplyDamage(player.State, 9, ESpatialOrientation.Forward);

		Assert.Equal(hullBefore - 6, player.State.HullPoints);
	}

	[Fact]
	public void ApplyDamage_ShieldChannelUnchangedByReinforcedHull()
	{
		var player = BattleTestFixture.Player(Coord.Zero);
		BattlePassiveStatusInstaller.InstallFromCapabilities(
			player.State,
			ActorRuntime.From(PilotBattleSnapshot.FromSkills("pilot", [EPilotSkillId.ReinforcedHull])));
		player.State.ShieldPoints[ESpatialOrientation.Forward] = 10;

		Defense.ApplyDamage(player.State, 6, ESpatialOrientation.Forward);

		Assert.Equal(4, player.State.ShieldPoints[ESpatialOrientation.Forward]);
		Assert.Equal(
			player.State.Loadout.MaxHullPoints,
			player.State.HullPoints);
	}
}

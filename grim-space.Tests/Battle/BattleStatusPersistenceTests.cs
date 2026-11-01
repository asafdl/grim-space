using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;
using GrimSpace.Run.Persistence;

namespace GrimSpace.Tests.StatusModel;

public sealed class BattleStatusPersistenceTests
{
	[Fact]
	public void FormatVersion_IsEight()
	{
		Assert.Equal(8, SaveGameDocument.CurrentFormatVersion);
	}

	[Fact]
	public void RoundTrip_RebuildsPassiveCallbacksFromInstanceId()
	{
		var state = BattleTestFixture.Player(Coord.Zero).State;
		BattlePassiveStatusInstaller.InstallFromCapabilities(
			state,
			ActorRuntime.From(PilotBattleSnapshot.FromSkills("pilot", [EPilotSkillId.ReinforcedHull])));

		var restored = SaveDtoMapper.RestoreBattleUnitState(SaveDtoMapper.CaptureBattleUnitState(state));

		Assert.Equal(67, BattleStatuses.RewriteHullDamage(restored.Statuses, 100));
	}

	[Fact]
	public void RoundTrip_RestoresHeatFlagsAsStatuses()
	{
		var state = BattleTestFixture.Player(Coord.Zero).State;
		state.Overdrive = true;
		state.Overheat = true;

		var restored = SaveDtoMapper.RestoreBattleUnitState(SaveDtoMapper.CaptureBattleUnitState(state));

		Assert.True(BattleStatuses.HasOverheat(restored));
		Assert.Single(
			restored.Statuses,
			status => status.InstanceId == BattleStatuses.BuffInstanceId("Overdrive", state.Id));
	}

	[Fact]
	public void RoundTrip_RestoresProtectionDroneBuff()
	{
		var state = BattleTestFixture.Player(Coord.Zero).State;
		state.Statuses.Add(BattleStatuses.CreateProtectionDrone(state.Id));

		var restored = SaveDtoMapper.RestoreBattleUnitState(SaveDtoMapper.CaptureBattleUnitState(state));

		var status = Assert.Single(restored.Statuses);
		Assert.NotNull(status.RewriteIncomingHit);
	}

	[Fact]
	public void RoundTrip_RestoresShieldBracePayload()
	{
		var state = BattleTestFixture.Player(Coord.Zero).State;
		state.ShieldPoints[ESpatialOrientation.Forward] = 50;
		ShieldBrace.Activate(state, ESpatialOrientation.Forward);
		ShieldBrace.TryPayload(state)!.DamagePending = 12;

		var restored = SaveDtoMapper.RestoreBattleUnitState(SaveDtoMapper.CaptureBattleUnitState(state));

		var payload = ShieldBrace.TryPayload(restored);
		Assert.NotNull(payload);
		Assert.Equal(ESpatialOrientation.Forward, payload.Face);
		Assert.Equal(12, payload.DamagePending);
		Assert.Equal(50, payload.Snapshot[ESpatialOrientation.Forward]);
		Assert.True(restored.ShieldBraceUsedThisTurn);
	}

	[Fact]
	public void RoundTrip_RestoresTemporaryShieldGrantsInOrder()
	{
		var state = BattleTestFixture.Player(Coord.Zero).State;
		TemporaryShields.GrantPerFace(state, "a", 3);
		TemporaryShields.GrantPerFace(state, "b", 5);

		var restored = SaveDtoMapper.RestoreBattleUnitState(SaveDtoMapper.CaptureBattleUnitState(state));

		var grants = TemporaryShields.ExtractGrants(restored);
		Assert.Equal(["a", "b"], grants.Select(grant => grant.GrantId));
		Assert.Equal(8, TemporaryShields.TotalOnFace(restored, ESpatialOrientation.Forward));
	}

	[Fact]
	public void Rehydrate_UnknownKindThrows()
	{
		var unknown = new BattleStatus
		{
			InstanceId = "buff:Mystery:player",
			Priority = 0,
			ActionTags = [],
		};

		Assert.Throws<InvalidDataException>(() => BattleStatusRehydrator.Rehydrate(unknown));
	}

	[Fact]
	public void Rehydrate_PayloadKindWithoutPayloadThrows()
	{
		var bare = new BattleStatus
		{
			InstanceId = "buff:ShieldBrace:player",
			Priority = 50,
			ActionTags = [],
		};

		Assert.Throws<InvalidDataException>(() => BattleStatusRehydrator.Rehydrate(bare));
	}
}

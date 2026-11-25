using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;
using GrimSpace.Run.Persistence;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.StatusModel;

public sealed class BattleStatusPersistenceTests
{
	[Fact]
	public void Load_RejectsPreviousStatusFormat()
	{
		var document = SaveGameDocument.Create("0.1.0", "battle", new { })
			with { FormatVersion = 13 };

		Assert.Equal(
			LoadResult.UnsupportedFormat,
			SaveLoadResult.Classify(SaveStorageResult.Success, document));
	}

	[Fact]
	public void RoundTrip_RebuildsPassiveTypedStatus()
	{
		var state = BattleTestFixture.Player(Coord.Zero).State;
		state.Statuses.Add(
			BattleStatusMaterializer.Create(EPilotCapabilityId.ReinforcedHull));

		var restored = SaveDtoMapper.RestoreBattleUnitState(SaveDtoMapper.CaptureBattleUnitState(state));

		Assert.Contains(restored.Statuses, status => status is ReinforcedHull);
	}

	[Fact]
	public void RoundTrip_RestoresHeatFlagsWithoutStatuses()
	{
		var state = BattleTestFixture.Player(Coord.Zero).State;
		state.Overdrive = true;
		state.Overheat = true;

		var restored = SaveDtoMapper.RestoreBattleUnitState(SaveDtoMapper.CaptureBattleUnitState(state));

		Assert.True(restored.Overdrive);
		Assert.True(restored.Overheat);
		Assert.Empty(restored.Statuses);
	}

	[Fact]
	public void RoundTrip_PreservesNullHeatCapacityForVoidBomb()
	{
		var state = ActorState.FromShipInstance(
			ShipInstance.FromCatalog("bomb", EType.VoidBomb),
			Coord.Zero);

		var restored = SaveDtoMapper.RestoreBattleUnitState(SaveDtoMapper.CaptureBattleUnitState(state));

		Assert.Null(restored.HeatProfile.Capacity);
		Assert.False(restored.HeatProfile.Participates);
		Assert.Equal(0, restored.HeatProfile.ClearPerTurn);
		Assert.Equal(1f, restored.HeatProfile.GainMultiplier);
	}

	[Fact]
	public void RoundTrip_RestoresProtectionDroneBuff()
	{
		var state = BattleTestFixture.Player(Coord.Zero).State;
		state.Statuses.Add(new ProtectionDroneBuff());

		var restored = SaveDtoMapper.RestoreBattleUnitState(SaveDtoMapper.CaptureBattleUnitState(state));

		Assert.IsType<ProtectionDroneBuff>(Assert.Single(restored.Statuses));
	}

	[Fact]
	public void TimelineRoundTrip_RehydratesProtectionDroneFactStatus()
	{
		var registry = PersistenceRegistry.CreateDefault();
		var status = new ProtectionDroneBuff();
		var written = registry.Write(
			new GrimSpace.Core.Actions.Record<GrimSpace.Battle.Effects.ProtectionDroneChangedFacts>(
				new("player", "ally", status)));

		var restored = Assert.IsType<GrimSpace.Core.Actions.Record<GrimSpace.Battle.Effects.ProtectionDroneChangedFacts>>(
			registry.Read(written));
		Assert.IsType<ProtectionDroneBuff>(restored.Value.Status);
	}

	[Fact]
	public void RoundTrip_RestoresMarkWeakspotFace()
	{
		var state = BattleTestFixture.Player(Coord.Zero).State;
		state.Statuses.Add(new MarkWeakspotDebuff { Face = ESpatialOrientation.Port });

		var restored = SaveDtoMapper.RestoreBattleUnitState(SaveDtoMapper.CaptureBattleUnitState(state));

		var payload = MarkWeakspot.TryStatus(restored);
		Assert.NotNull(payload);
		Assert.Equal(ESpatialOrientation.Port, payload.Face);
	}

	[Fact]
	public void RoundTrip_RestoresShieldBracePayload()
	{
		var state = BattleTestFixture.Player(Coord.Zero).State;
		state.CurrentShieldPoints[ESpatialOrientation.Forward] = 50;
		ShieldBrace.Activate(state, ESpatialOrientation.Forward);
		ShieldBrace.TryStatus(state)!.DamagePending = 12;

		var restored = SaveDtoMapper.RestoreBattleUnitState(SaveDtoMapper.CaptureBattleUnitState(state));

		var payload = ShieldBrace.TryStatus(restored);
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
	public void FromDto_UnknownTypeThrows()
	{
		Assert.Throws<NotSupportedException>(() =>
			BattleStatusJsonConverter.FromDto(new BattleStatusDto("Mystery", EStatusPolarity.Buff)));
	}
}

using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.Units;
using GrimSpace.Components;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Units.Loadouts.Defenses;

namespace GrimSpace.Tests.Ui;

[BattleTestSuite]
public sealed class BattleStatusDisplayTests
{
	[Fact]
	public void NoStatusesProducesNoIcons()
	{
		var unit = Capture(CreateState());

		Assert.Empty(BattleStatusDisplay.Capture(unit));
		Assert.Empty(BattleStatusDisplay.Capture(unit with { Statuses = null }));
	}

	[Fact]
	public void ReactiveShieldsCounterIsCapturedAndRoundTripsWithoutSharingState()
	{
		var state = CreateState();
		state.ReactiveShieldsStacks = 3;
		state.Statuses.Add(BattlePassiveStatusInstaller.Create(EPilotCapabilityId.ReactiveShields, state.Id));

		var unit = Capture(state);
		state.ReactiveShieldsStacks = 0;
		state.Statuses.Clear();

		var display = Assert.Single(BattleStatusDisplay.Capture(unit));
		Assert.Equal(3, display.StackCount);
		Assert.Equal("Reactive Shields", display.Tooltip.Title);
		Assert.Contains("multiplicatively", display.Tooltip.Details);
		Assert.Equal(3, unit.ToState().ReactiveShieldsStacks);
		Assert.Single(unit.ToState().Statuses);
	}

	[Fact]
	public void ReactiveShieldsShowsZeroAfterResetRatherThanInventingOneStack()
	{
		var state = CreateState();
		state.Statuses.Add(BattlePassiveStatusInstaller.Create(EPilotCapabilityId.ReactiveShields, state.Id));

		Assert.Equal(0, Assert.Single(BattleStatusDisplay.Capture(Capture(state))).StackCount);
	}

	[Fact]
	public void TemporaryShieldGrantsShareAnIconWithActualGrantCountAndRemainingShields()
	{
		var state = CreateState();
		TemporaryShields.GrantPerFace(state, "first", 10);
		TemporaryShields.GrantPerFace(state, "second", 20);
		TemporaryShields.AbsorbOnFace(state, ESpatialOrientation.Forward, 12);
		var unit = Capture(state);
		TemporaryShields.AbsorbOnFace(state, ESpatialOrientation.Forward, 18);

		var display = Assert.Single(BattleStatusDisplay.Capture(unit));
		Assert.Equal("buff:TemporaryShield", display.Key);
		Assert.Equal(2, display.StackCount);
		Assert.Contains("Forward: 18", display.Tooltip.Details);
		Assert.Contains("Dorsal: 30", display.Tooltip.Details);
	}

	[Fact]
	public void BraceShowsChosenFaceAndPendingDamageWithoutAStackCounter()
	{
		var state = CreateState();
		state.Statuses.Add(ShieldBrace.CreateStatus(
			state.Id, ESpatialOrientation.Port, new FaceShieldPoints(), 42));

		var display = Assert.Single(BattleStatusDisplay.Capture(Capture(state)));

		Assert.Null(display.StackCount);
		Assert.Equal("Brace!", display.Tooltip.Title);
		Assert.Contains("Port", display.Tooltip.Details);
		Assert.Contains("42", display.Tooltip.Details);
	}

	[Fact]
	public void OverdriveUsesTheDisplayedShipsDamageBonusAndOverheatIsADebuff()
	{
		var state = CreateState();
		state.Statuses.Add(BattleStatuses.CreateOverdrive(state.Id));
		state.Statuses.Add(BattleStatuses.CreateOverheat(state.Id));

		var display = BattleStatusDisplay.Capture(Capture(state));

		Assert.Equal(2, display.Count);
		Assert.Contains($"{state.Loadout.OverdriveDamageBonusPercent}%", display[0].Tooltip.Body);
		Assert.Equal(EStatusPolarity.Buff, display[0].Polarity);
		Assert.Equal(EStatusPolarity.Debuff, display[1].Polarity);
		Assert.All(display, entry => Assert.Null(entry.StackCount));
	}

	[Theory]
	[InlineData(EPilotCapabilityId.ScrapDroneSwarmDamageBonus, "Scrap Drone")]
	[InlineData(EPilotCapabilityId.HealingSkillRangeBonus, "Help a Botter Out")]
	[InlineData(EPilotCapabilityId.LightningShieldDamageBonus, "Ionized Payload")]
	[InlineData(EPilotCapabilityId.AdjacentDamageBonus, "Dodge This")]
	[InlineData(EPilotCapabilityId.ReinforcedHull, "Reinforced Hull")]
	[InlineData(EPilotCapabilityId.InTheFray, "In The Fray")]
	[InlineData(EPilotCapabilityId.ReactiveShields, "Reactive Shields")]
	[InlineData(EPilotCapabilityId.StationaryDamageDeflection, "Defensive Maneuvers")]
	[InlineData(EPilotCapabilityId.ShieldDynamo, "Shield Dynamo")]
	[InlineData(EPilotCapabilityId.ThirstForScrap, "Thirst For Scrap")]
	[InlineData(EPilotCapabilityId.YouLittleRat, "You Little Rat")]
	[InlineData(EPilotCapabilityId.NothingButMetal, "Nothing But Metal")]
	[InlineData(EPilotCapabilityId.SpawnedUnitDamageBonus, "Not Fully Formed")]
	[InlineData(EPilotCapabilityId.OneWithTheVoid, "One With The Void")]
	[InlineData(EPilotCapabilityId.OutgoingDamageBonus, "Asteroid Surfer")]
	[InlineData(EPilotCapabilityId.IncreasedHullDamageTaken, "Asteroid Surfer")]
	[InlineData(EPilotCapabilityId.SpawnedAllyDamageBonus, "Retired Machine Manager")]
	public void EveryInstalledPassiveHasAnIconAndEffectDescription(EPilotCapabilityId capability, string title)
	{
		var state = CreateState();
		state.Statuses.Add(BattlePassiveStatusInstaller.Create(capability, state.Id));

		var display = Assert.Single(BattleStatusDisplay.Capture(Capture(state)));

		Assert.Equal(title, display.Tooltip.Title);
		Assert.False(string.IsNullOrWhiteSpace(display.Tooltip.Body));
		Assert.False(string.IsNullOrWhiteSpace(display.IconName));
	}

	[Fact]
	public void ProtectionDroneReusesTheChassisAbilityDescription()
	{
		var state = CreateState();
		state.Statuses.Add(BattleStatuses.CreateProtectionDrone(state.Id));

		Assert.Equal(
			(HudTooltipContent)BattleHudCopy.ProtectionDroneTooltipFor(ProtectionDroneSpec.Baseline),
			Assert.Single(BattleStatusDisplay.Capture(Capture(state))).Tooltip);
	}

	[Theory]
	[InlineData(0, "+0%")]
	[InlineData(3, "+30%")]
	public void ThirstForScrapDisplaysCapturedStacksAndDamageBonus(int stacks, string bonus)
	{
		var state = CreateState();
		state.ThirstForScrapStacks = stacks;
		state.Statuses.Add(BattlePassiveStatusInstaller.Create(EPilotCapabilityId.ThirstForScrap, state.Id));
		var unit = Capture(state);
		state.ThirstForScrapStacks = 99;

		var display = Assert.Single(BattleStatusDisplay.Capture(unit));

		Assert.Equal(stacks, display.StackCount);
		Assert.Equal("Damage", display.IconName);
		Assert.Contains(bonus, display.Tooltip.Details);
		Assert.Equal(stacks, unit.ToState().ThirstForScrapStacks);
	}

	[Fact]
	public void IconsFollowTheSuppliedShipRatherThanThePlayer()
	{
		var player = CreateState();
		var enemy = BattleTestFixture.Enemy(new Coord(5, 5, 7)).State;
		enemy.Statuses.Add(BattleStatuses.CreateOverheat(enemy.Id));

		Assert.Empty(BattleStatusDisplay.Capture(Capture(player)));
		Assert.Equal("Overheat", Assert.Single(BattleStatusDisplay.Capture(Capture(enemy))).Tooltip.Title);
	}

	[Fact]
	public void UnknownStatusesFailExplicitly()
	{
		var state = CreateState();
		state.Statuses.Add(new BattleStatus
		{
			InstanceId = BattleStatuses.BuffInstanceId("Unknown", state.Id),
			Priority = 0,
			ActionTags = [],
		});

		Assert.Throws<InvalidDataException>(() => BattleStatusDisplay.Capture(Capture(state)));
	}

	private static State CreateState() => BattleTestFixture.Player(new Coord(5, 5, 5)).State;

	private static UnitDisplayState Capture(State state) => UnitDisplayState.Capture(state, new ActorRuntime());
}

using GrimSpace.World.Factions;

namespace GrimSpace.Tests.World.Factions;

[BattleTestSuite]
public sealed class FactionRelationsTests
{
	[Theory]
	[InlineData(EFaction.Player, EFaction.Pirates)]
	[InlineData(EFaction.Pirates, EFaction.Player)]
	public void IsHostile_PlayerAndPirates_AreHostile(EFaction left, EFaction right)
	{
		Assert.True(FactionRelations.IsHostile(left, right));
	}

	[Theory]
	[InlineData(EFaction.Player, EFaction.TheOptimality)]
	[InlineData(EFaction.TheOptimality, EFaction.Player)]
	[InlineData(EFaction.Pirates, EFaction.TheOptimality)]
	[InlineData(EFaction.TheOptimality, EFaction.Pirates)]
	public void IsHostile_NonHostilePairs_ReturnFalse(EFaction left, EFaction right)
	{
		Assert.False(FactionRelations.IsHostile(left, right));
	}

	[Theory]
	[InlineData(EFaction.Player)]
	[InlineData(EFaction.TheOptimality)]
	[InlineData(EFaction.Pirates)]
	public void IsHostile_SameFaction_ReturnsFalse(EFaction faction)
	{
		Assert.False(FactionRelations.IsHostile(faction, faction));
	}

	[Fact]
	public void IsHostile_IsSymmetric()
	{
		foreach (var left in Enum.GetValues<EFaction>())
		{
			foreach (var right in Enum.GetValues<EFaction>())
				Assert.Equal(
					FactionRelations.IsHostile(left, right),
					FactionRelations.IsHostile(right, left));
		}
	}

	[Theory]
	[InlineData((EFaction)(-1), EFaction.Player)]
	[InlineData(EFaction.Player, (EFaction)(-1))]
	[InlineData((EFaction)99, EFaction.Player)]
	[InlineData(EFaction.Player, (EFaction)99)]
	public void IsHostile_InvalidFaction_Throws(EFaction left, EFaction right)
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => FactionRelations.IsHostile(left, right));
	}
}

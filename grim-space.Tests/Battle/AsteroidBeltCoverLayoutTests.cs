using GrimSpace.Battle.NonUnits;
using GrimSpace.Battle.Presentation.Graphics;

namespace GrimSpace.Tests.Hazards;

[BattleTestSuite]
public sealed class AsteroidBeltCoverLayoutTests
{
	[Fact]
	public void Create_UsesMoreRocksForHigherResistanceAndSharesTheSamePrefix()
	{
		var low = AsteroidBeltCoverLayout.Create(AsteroidBelt.LowResistancePercent, 19);
		var high = AsteroidBeltCoverLayout.Create(AsteroidBelt.HighResistancePercent, 19);

		Assert.NotEmpty(low);
		Assert.True(high.Count > low.Count);
		Assert.Equal(low, high.Take(low.Count));
	}

	[Fact]
	public void Create_IsDeterministicAndKeepsEachRockInsideOneCell()
	{
		var first = AsteroidBeltCoverLayout.Create(AsteroidBelt.HighResistancePercent, 19);
		var second = AsteroidBeltCoverLayout.Create(AsteroidBelt.HighResistancePercent, 19);

		Assert.Equal(first, second);
		Assert.True(first.Max(rock => MathF.Abs(rock.Y)) >= 0.25f);
		Assert.All(first, rock =>
		{
			Assert.True(rock.Diameter > 0f);
			Assert.InRange(MathF.Abs(rock.X), 0f, 0.52f);
			Assert.InRange(MathF.Abs(rock.Y), 0f, 0.52f);
			Assert.InRange(MathF.Abs(rock.Z), 0f, 0.52f);
			Assert.True(MathF.Abs(rock.X) + rock.Diameter / 2f <= 0.58f);
			Assert.True(MathF.Abs(rock.Y) + rock.Diameter / 2f <= 0.58f);
			Assert.True(MathF.Abs(rock.Z) + rock.Diameter / 2f <= 0.58f);
		});
		Assert.Contains(first, rock =>
			MathF.Abs(rock.X) + rock.Diameter / 2f > 0.5f
			|| MathF.Abs(rock.Y) + rock.Diameter / 2f > 0.5f
			|| MathF.Abs(rock.Z) + rock.Diameter / 2f > 0.5f);
	}

	[Fact]
	public void Create_UsesMoreAndSmallerRocksForBothResistanceLevels()
	{
		var low = AsteroidBeltCoverLayout.Create(AsteroidBelt.LowResistancePercent, 19);
		var high = AsteroidBeltCoverLayout.Create(AsteroidBelt.HighResistancePercent, 19);

		Assert.True(low.Count >= 24);
		Assert.True(high.Count >= 48);
		Assert.All(low.Concat(high), rock => Assert.InRange(rock.Diameter, 0f, 0.12f));
	}

	[Theory]
	[InlineData(0)]
	[InlineData(100)]
	public void Create_RejectsUnsupportedResistance(int resistancePercent)
	{
		Assert.Throws<ArgumentOutOfRangeException>(
			() => AsteroidBeltCoverLayout.Create(resistancePercent, 19));
	}
}

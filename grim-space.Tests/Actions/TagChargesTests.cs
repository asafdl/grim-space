using GrimSpace.Core.Actions;

namespace GrimSpace.Tests.Actions;

public sealed class TagChargesTests
{
	[Fact]
	public void AddAndGetAccumulateCharges()
	{
		var charges = new TagCharges();

		charges.Add("alpha", 1);
		charges.Add("alpha", 2);

		Assert.Equal(3, charges.Get("alpha"));
	}

	[Fact]
	public void TryConsumeRemovesChargesWhenAvailable()
	{
		var charges = new TagCharges();
		charges.Set("beta", 2);

		Assert.True(charges.TryConsume("beta", 1));
		Assert.Equal(1, charges.Get("beta"));
		Assert.True(charges.TryConsume("beta", 1));
		Assert.Equal(0, charges.Get("beta"));
	}

	[Fact]
	public void TryConsumeFailsWhenInsufficientCharges()
	{
		var charges = new TagCharges();
		charges.Set("gamma", 1);

		Assert.False(charges.TryConsume("gamma", 2));
		Assert.Equal(1, charges.Get("gamma"));
	}

	[Fact]
	public void ClearRemovesAllTags()
	{
		var charges = new TagCharges();
		charges.Add("delta", 1);
		charges.Clear();

		Assert.Equal(0, charges.Get("delta"));
	}

	[Theory]
	[InlineData(0, 0)]
	[InlineData(1, 1)]
	[InlineData(4, 0)]
	[InlineData(-1, 3)]
	[InlineData(5, 1)]
	public void GetNormalizedWrapsToModRange(int value, int expected)
	{
		var charges = new TagCharges();
		charges.Set("yaw", value);

		Assert.Equal(expected, charges.GetNormalized("yaw", 4));
	}
}

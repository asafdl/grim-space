using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Tests.Units.Loadouts.Abilities;

public sealed class AbilityLoadoutTests
{
	[Fact]
	public void AbilityDefinition_UnknownId_Throws()
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => AbilityDefinition.For((EAbilityKind)999));
	}
}

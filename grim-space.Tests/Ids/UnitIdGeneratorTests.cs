using GrimSpace.Battle.Ids;
using GrimSpace.Core.Ids;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Ids;

public sealed class UnitIdGeneratorTests
{
	[Fact]
	public void GeneratedIdsMatchFormatAreUnique()
	{
		var generated = new HashSet<string>();

		for (var i = 0; i < 32; i++)
		{
			var id = TypedIdGenerator.NextId(UnitTypeSlug.For(EType.Fighter));
			Assert.StartsWith("fighter-", id);
			Assert.True(generated.Add(id), $"Duplicate id generated: {id}");
		}
	}

	[Fact]
	public void TypedIdGenerator_FormatMatchesTypeAdjAnimal()
	{
		var id = TypedIdGenerator.Format("poi", "swift-fox");
		Assert.Equal("poi-swift-fox", id);

		var generated = TypedIdGenerator.NextId("station");
		Assert.StartsWith("station-", generated);
		Assert.Equal(2, generated.Count(c => c == '-'));
	}
}

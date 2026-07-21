using GrimSpace.Battle.Ids;
using GrimSpace.Core;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Ids;

public sealed class UnitIdGeneratorTests
{
	[Fact]
	public void GeneratedIdsMatchFormatAreUniqueAndNeverBoardId()
	{
		var registry = new UnitIdRegistry();
		var ids = new HashSet<string>();

		for (var i = 0; i < 32; i++)
		{
			var id = registry.NextUnitId(EType.Fighter);
			Assert.StartsWith("fighter-", id);
			Assert.NotEqual(EntityIds.World, id);
			Assert.NotEqual(EntityIds.System, id);
			Assert.True(ids.Add(id), $"Duplicate id generated: {id}");
		}
	}
}

using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Player;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests;

public sealed class BattleEngagementCompositionTests
{
	[Fact]
	public void EnsurePlayable_RejectsSingleTeamSpawns()
	{
		var spawns = new[]
		{
			new BattleSpawn
			{
				Ship = ShipInstance.FromCatalog("solo-player", EType.Fighter),
				Team = ETeam.Player,
				Position = Coord.Zero,
				Fore = Coord.Forward,
				Dorsal = Coord.Up,
				ExecutionAgent = new UserExecutionAgent(),
			},
		};

		var ex = Assert.Throws<InvalidOperationException>(
			() => BattleEngagementComposition.EnsurePlayable(spawns, "engagement-test"));
		Assert.Contains("engagement-test", ex.Message, StringComparison.Ordinal);
		Assert.Contains("enemy=0", ex.Message, StringComparison.Ordinal);
	}
}

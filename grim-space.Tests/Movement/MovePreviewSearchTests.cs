using GrimSpace.Battle.Movement;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Movement;

public sealed class MovePreviewSearchTests
{
	private const string PlayerId = "player";

	[Fact]
	public void PreviewIsIndependentOfLegacyMomentum()
	{
		var origin = new Coord(8, 8, 8);
		var withoutMomentum = Discover(origin, momentum: 0);
		var withMomentum = Discover(origin, momentum: 2);

		Assert.Equal(
			withoutMomentum.Select(Key).OrderBy(key => key),
			withMomentum.Select(Key).OrderBy(key => key));
	}

	[Fact]
	public void DiscoverExtensionsRetainsOneRoutePerEndPose()
	{
		var paths = Discover(new Coord(5, 5, 5));

		Assert.Equal(
			paths.Select(Key).Distinct().Count(),
			paths.Count);
		Assert.Contains(paths.GroupBy(path => path.EndPosition), group => group.Count() > 1);
	}

	[Fact]
	public void DefaultOrderingIsDeterministic()
	{
		var origin = new Coord(5, 5, 5);

		Assert.Equal(
			Discover(origin).Select(path => string.Join('|', path.Steps)),
			Discover(origin).Select(path => string.Join('|', path.Steps)));
	}

	[Fact]
	public void CurrentPositionRequiresANonemptyReturnRoute()
	{
		var origin = new Coord(5, 5, 5);
		var paths = Discover(origin);

		Assert.All(paths.Where(path => path.EndPosition == origin), path => Assert.NotEmpty(path.Steps));
		Assert.Contains(paths, path => path.EndPosition == origin);
	}

	private static string Key(MovePathSession path) =>
		$"{path.EndPosition}|{path.EndBasis.Forward}|{path.EndBasis.Up}";

	private static IReadOnlyList<MovePathSession> Discover(Coord origin, int momentum = 0)
	{
		var battle = BattleTestFixture.BeginSimulation(
			BattleTestFixture.Player(origin, momentum: momentum),
			BattleTestFixture.Enemy(new Coord(0, 0, 0)),
			BattleTestFixture.Grid(20));
		return MovePathEndpoints.DiscoverExtensions(battle.PlayerAgent.Sim, PlayerId);
	}
}

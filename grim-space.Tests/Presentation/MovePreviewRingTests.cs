using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Math.Grid;

namespace grim_space.Tests.Presentation;

public sealed class MovePreviewRingTests
{
	private static readonly Coord Actor = Coord.Zero;

	private static Option OptionAt(Coord end, int apCost) =>
		new() { ApCost = apCost, Path = new List<Coord> { end } };

	[Fact]
	public void BuildRingTable_twoShells_ringCountAndSortedK()
	{
		var options = new List<Option>
		{
			OptionAt(new Coord(2, 0, 0), 1),
			OptionAt(new Coord(1, 0, 0), 1),
		};

		var table = MovePreviewRings.BuildRingTable(Actor, options);

		Assert.Equal(2, table.RingCount);
		Assert.Equal(1, table.ShellK(0));
		Assert.Equal(2, table.ShellK(1));
	}

	[Fact]
	public void BuildRingTable_dedupeByEndPosition_keepsLowerApCost()
	{
		var end = new Coord(1, 0, 0);
		var options = new List<Option>
		{
			OptionAt(end, 5),
			OptionAt(end, 2),
		};

		var table = MovePreviewRings.BuildRingTable(Actor, options);

		Assert.Equal(1, table.RingCount);
		Assert.Equal(new[] { 1 }, table.OptionIndicesOnRing(0));
	}

	[Fact]
	public void BuildRingTable_equalApCost_keepsLowerOptionIndex()
	{
		var end = new Coord(1, 0, 0);
		var options = new List<Option>
		{
			OptionAt(end, 3),
			OptionAt(end, 3),
		};

		var table = MovePreviewRings.BuildRingTable(Actor, options);

		Assert.Equal(new[] { 0 }, table.OptionIndicesOnRing(0));
	}

	[Fact]
	public void BuildRingTable_skipsEmptyKBetweenUsedShells()
	{
		var options = new List<Option>
		{
			OptionAt(new Coord(1, 0, 0), 1),
			OptionAt(new Coord(3, 0, 0), 1),
		};

		var table = MovePreviewRings.BuildRingTable(Actor, options);

		Assert.Equal(2, table.RingCount);
		Assert.Equal(1, table.ShellK(0));
		Assert.Equal(3, table.ShellK(1));
	}

	[Fact]
	public void BuildRingTable_emptyOptions_zeroRings()
	{
		var table = MovePreviewRings.BuildRingTable(Actor, Array.Empty<Option>());

		Assert.Equal(0, table.RingCount);
	}

	[Fact]
	public void BuildRingTable_optionIndicesOnRing_sortedByOptionIndex()
	{
		var options = new List<Option>
		{
			OptionAt(new Coord(2, 0, 0), 1),
			OptionAt(new Coord(0, 2, 0), 1),
			OptionAt(new Coord(0, 0, 2), 1),
		};

		var table = MovePreviewRings.BuildRingTable(Actor, options);

		Assert.Equal(1, table.RingCount);
		Assert.Equal(new[] { 0, 1, 2 }, table.OptionIndicesOnRing(0));
	}

	[Fact]
	public void BuildRingTable_partitionsIndicesByRing()
	{
		var options = new List<Option>
		{
			OptionAt(new Coord(1, 0, 0), 1),
			OptionAt(new Coord(2, 0, 0), 1),
			OptionAt(new Coord(0, 2, 0), 1),
		};

		var table = MovePreviewRings.BuildRingTable(Actor, options);

		Assert.Equal(2, table.RingCount);
		Assert.Equal(new[] { 0 }, table.OptionIndicesOnRing(0));
		Assert.Equal(new[] { 1, 2 }, table.OptionIndicesOnRing(1));
	}
}

using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation.Domains.Move;
using GrimSpace.Battle.Presentation.Picking;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using Godot;

namespace GrimSpace.Tests.Presentation;

public sealed class MovementHoverReuseTests
{
	[Fact]
	public void ReuseRequiresMatchingPointerCameraViewportAndRoute()
	{
		var basis = MovePose.For(Coord.Forward, 0);
		var option = CreateOption(new Coord(3, 4, 5), basis, stepCount: 2);
		var snapshot = new MovementHoverSnapshot(
			new Vector2(100f, 200f),
			option.EndPosition,
			option.EndBasis,
			option.Steps.Count,
			Transform3D.Identity,
			new Vector2I(1920, 1080));

		Assert.True(MovementHoverReuse.CanReuseDisplayedHover(
			snapshot,
			new Vector2(100f, 200f),
			Transform3D.Identity,
			new Vector2I(1920, 1080),
			option));
	}

	[Fact]
	public void ReuseFailsWhenPointerMoves()
	{
		var option = CreateOption(new Coord(1, 2, 3), MovePose.For(Coord.Forward, 0), stepCount: 1);
		var snapshot = new MovementHoverSnapshot(
			new Vector2(10f, 20f),
			option.EndPosition,
			option.EndBasis,
			option.Steps.Count,
			Transform3D.Identity,
			new Vector2I(800, 600));

		Assert.False(MovementHoverReuse.CanReuseDisplayedHover(
			snapshot,
			new Vector2(11f, 20f),
			Transform3D.Identity,
			new Vector2I(800, 600),
			option));
	}

	private static MovePathOption CreateOption(Coord end, GridBasis basis, int stepCount)
	{
		var steps = Enumerable.Range(0, stepCount)
			.Select(_ => (IAction)new MoveStepAction("player"))
			.ToList();
		return new MovePathOption(
			steps,
			[],
			end,
			basis,
			ExtensionApCost: 0,
			RemainingAp: 0,
			ResultState: default);
	}
}

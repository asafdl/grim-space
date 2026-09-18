using Godot;
using GrimSpace.Battle.Presentation.Camera;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Education;
using GrimSpace.Math.Camera;

namespace GrimSpace.Battle.Presentation;

public sealed class BattleWorldFocus(
	Controller camera,
	BattleLayout layout,
	BattleView battleView,
	Func<ActorState> playerState) : IWorldFocus
{
	public WorldFocusResult Focus(string objectId)
	{
		_ = camera;
		_ = layout;
		_ = battleView;
		_ = playerState;
		return new WorldFocusResult.Accepted(new NoOpFocusHandle());
	}

	private sealed class NoOpFocusHandle : IWorldFocusHandle
	{
		public void Dispose()
		{
		}
	}
}

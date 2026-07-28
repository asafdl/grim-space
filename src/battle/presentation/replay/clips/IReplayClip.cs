using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Presentation.Replay.Clips;

public interface IReplayClip
{
	Type ActionType { get; }

	ClipPlayback Play(IAction action, ReplayClipContext context);
}

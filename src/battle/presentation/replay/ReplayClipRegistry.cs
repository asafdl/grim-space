using GrimSpace.Battle.Presentation.Replay.Clips;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Presentation.Replay;

public sealed class ReplayClipRegistry
{
	public static ReplayClipRegistry Default { get; } = new(
	[
		new MoveStepClip(),
		new HeadingTurnClip(),
		new RollClip(),
		new TorpedoActionClip(),
		new SpawnPatrolActionClip(),
		new RailgunActionClip(),
		new FlakActionClip(),
		new DetonateActionClip(),
	]);

	private readonly Dictionary<Type, IReplayClip> _clips;

	public ReplayClipRegistry(IEnumerable<IReplayClip> clips) =>
		_clips = clips.ToDictionary(clip => clip.ActionType);

	public bool TryPlay(IAction action, ReplayClipContext context, out ClipPlayback playback)
	{
		if (_clips.TryGetValue(action.GetType(), out var clip))
		{
			playback = clip.Play(action, context);
			return true;
		}

		playback = ClipPlayback.Instant;
		return false;
	}
}

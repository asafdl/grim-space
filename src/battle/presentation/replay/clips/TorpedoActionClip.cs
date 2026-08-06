using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Presentation.Replay.Clips;

public sealed class TorpedoActionClip : IReplayClip
{
	public Type ActionType => typeof(TorpedoAction);

	public ClipPlayback Play(IAction action, ReplayClipContext context)
	{
		var fire = (TorpedoAction)action;
		var firer = context.ReplayState.StateOf(fire.ActorId);
		var (position, fore, dorsal) = TorpedoMount.LaunchPose(firer, fire.Mount);

		var spawnedId = context.EndStates
			.Where(pair => pair.Value.Type == EType.Torpedo && !context.ReplayState.Contains(pair.Key))
			.Select(pair => pair.Key)
			.FirstOrDefault()
			?? throw new InvalidOperationException("TorpedoAction replay missing spawned unit in EndStates.");

		var spawned = context.EndStates[spawnedId].Clone();
		spawned.Position = position;
		spawned.Fore = fore;
		spawned.Dorsal = dorsal;
		spawned.Starboard = Coord.Cross(dorsal, fore);
		spawned.FuelRemaining = TorpedoConfig.Fuel;
		spawned.MomentumLevel = TorpedoConfig.SpawnMomentum;
		spawned.HullPoints = spawned.Stats.MaxHullPoints;
		spawned.ActionPoints = spawned.Stats.MaxAp;

		context.ReplayState.Add(spawned);
		context.EnsureView(spawned, context.ColorFor(spawned.Id));
		context.UnitViews[spawned.Id].Sync(spawned);
		return ClipPlayback.Instant;
	}
}

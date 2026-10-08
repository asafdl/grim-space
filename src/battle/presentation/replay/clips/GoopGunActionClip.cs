using Godot;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Presentation.Camera;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Battle.Presentation.Replay;
using GrimSpace.Battle.Spatial;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Battle.Presentation.Replay.Clips;

public sealed class GoopGunActionClip : IReplayClip
{
	public Type ActionType => typeof(GoopGunAction);

	public ClipPlayback Play(IAction action, ReplayClipContext context)
	{
		var goopGun = (GoopGunAction)action;
		var state = context.ReplayState.StateOf(goopGun.ActorId);
		var spec = state.FindInstalled(EAbilityKind.GoopGun, goopGun.MountedOn)?.Spec as GoopGunSpec
			?? throw new InvalidOperationException(
				$"Goop gun replay actor '{goopGun.ActorId}' has no goop gun installed on {goopGun.MountedOn}.");
		var frame = BodyFrame.From(state);
		var sourceCell = state.Position + frame.Step(goopGun.MountedOn);
		var targetCell = state.Position + frame.Fore * spec.Range;
		var source = WorldMapping.ToWorld(sourceCell);
		var target = WorldMapping.ToWorld(targetCell);

		context.ReportInterest?.Invoke(new CameraInterest(
			[source, target],
			CameraImportance.Combat));
		GoopSprayEffect.Play(
			context.HazardBursts,
			source,
			target,
			SeedFor(goopGun.GoopHazardId));

		return ClipPlayback.Pause(GoopSprayEffect.ImpactSeconds);
	}

	private static ulong SeedFor(string id)
	{
		const ulong offset = 14695981039346656037UL;
		const ulong prime = 1099511628211UL;
		var hash = offset;
		foreach (var character in id)
		{
			hash ^= character;
			hash *= prime;
		}

		return hash;
	}
}

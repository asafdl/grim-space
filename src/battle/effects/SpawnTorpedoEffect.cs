using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Battle.Effects;

public sealed class SpawnTorpedoEffect(AbilityMount mount, string unitId)
	: IEffect<BattleWorld, ActorRuntime>
{
	private Unit? _spawned;

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var units = UnitRegistry.For(world);
		var firer = units.UnitOf(actorId);
		var (position, fore, dorsal) = TorpedoMount.LaunchPose(firer.State, mount.Facet);
		var installed = firer.State.FindInstalled(mount.Kind, mount.Facet)
			?? throw new InvalidOperationException(
				$"No '{mount.Kind}' ability installed on facet '{mount.Facet}' for actor '{actorId}'.");
		if (installed.Spec is not TorpedoLauncherSpec launcher)
			throw new InvalidOperationException($"Mount '{mount}' is not a torpedo launcher.");
		var projectile = TorpedoProjectile.FromLauncher(launcher);
		var child = Factory.ChildFromSpawnableMount(firer.State, mount, unitId);
		var torpedo = Factory.Create(
			child,
			firer.Team,
			position,
			new TorpedoExecutionAgent(),
			fore,
			dorsal);
		torpedo.State.Projectile = projectile;
		torpedo.State.FuelRemaining = projectile.FuelTurns;
		torpedo.State.Stats = new Stats { MaxAp = projectile.MovementActionPoints };
		torpedo.State.ActionPoints = projectile.MovementActionPoints;
		torpedo.State.ParentId = actorId;
		units.Add(torpedo);
		_spawned = torpedo;

		return
		[
			new Record<SpawnFacts>(new SpawnFacts(
				SourceId: actorId,
				TargetId: torpedo.State.Id,
				EntityType: EType.Torpedo,
				SpawnedState: torpedo.State.Clone())),
		];
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		if (_spawned is null)
			return;

		UnitRegistry.For(world).Remove(_spawned.State.Id);
		_spawned = null;
	}
}

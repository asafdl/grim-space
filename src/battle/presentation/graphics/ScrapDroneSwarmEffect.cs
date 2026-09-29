using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Presentation.Replay;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Spatial;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Battle.Presentation.Graphics;

internal static class ScrapDroneAttackEffect
{
	private const string AttackScenePath = "res://assets/vfx/scrap_drone_attack.tscn";

	private static PackedScene? _attackScene;

	public static Vector3 MuzzleWorldPosition(ScrapDroneSwarmAction swarm, State firer)
	{
		var frame = BodyFrame.From(firer);
		return WorldMapping.ToWorld(firer.Position + frame.Step(swarm.MountedOn));
	}

	public static ScrapDroneAttack Play(
		Node3D parent,
		ScrapDroneSwarmAction swarm,
		ReplayState replayState,
		IReadOnlyDictionary<string, UnitView> unitViews)
	{
		var firer = replayState.StateOf(swarm.ActorId);
		var frame = BodyFrame.From(firer);
		var burstDirection = frame.Step(swarm.MountedOn);
		var starboard = ToVector3(firer.Starboard);
		var worldForward = swarm.MountedOn == ESpatialOrientation.Port ? -starboard : starboard;
		var worldUp = ToVector3(firer.Dorsal);
		var muzzle = MuzzleWorldPosition(swarm, firer);

		var swarmSpec = firer.FindInstalled(EAbilityKind.ScrapDroneSwarm, swarm.MountedOn)?.Spec as ScrapDroneSwarmSpec;
		var reachWorld = (swarmSpec is null
			? 3.6f
			: AbilityReach.ScrapDroneSwarmReplayShotLength(swarmSpec)) * WorldMapping.CellSize;
		var missEnd = muzzle + worldForward * reachWorld;

		var burstCells = swarmSpec is null
			? new HashSet<Coord>()
			: new HashSet<Coord>(swarmSpec.GetArea(frame.Origin, burstDirection, frame.Fore, frame.Dorsal));

		var hits = unitViews.Keys
			.Where(unitId => unitId != swarm.ActorId && replayState.Contains(unitId))
			.Select(unitId => replayState.StateOf(unitId))
			.Where(unit => unit.IsAlive && burstCells.Contains(unit.Position))
			.OrderBy(unit => unit.Id, StringComparer.Ordinal)
			.Select(unit => WorldMapping.ToWorld(unit.Position))
			.ToList();

		_attackScene ??= GD.Load<PackedScene>(AttackScenePath)
			?? throw new InvalidOperationException($"Could not load scrap drone VFX '{AttackScenePath}'.");

		PresentationSfx.PlayScrapDroneMount(parent, muzzle);

		var effect = _attackScene.Instantiate<ScrapDroneAttack>();
		parent.AddChild(effect);
		effect.Play(muzzle, worldForward, worldUp, hits, missEnd);
		return effect;
	}

	private static Vector3 ToVector3(Coord coord) =>
		new(coord.X, coord.Y, coord.Z);
}

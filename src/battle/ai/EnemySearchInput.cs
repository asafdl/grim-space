using System.Collections.Frozen;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Dfs;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Battle.Ai;

internal static class EnemySearchInput
{
	internal const int UnusedApPenalty = 100;
	internal const int DamageHitBonus = 2_000;
	internal const int GoopThreatBlockBonus = 2_500;
	internal const int TimelineRefinementSlack = UnusedApPenalty;
	internal const int FacingWeight = 800;
	internal const int ApproachWeight = 150;

	public static int ScoreHeuristic(
		SearchFrame<BattleWorld, ActorRuntime> frame,
		BattleSimulation anchor,
		string actorId,
		int searchStartDepth)
	{
		var world = frame.World.Fork();
		var runtimes = frame.Runtimes.Fork();
		ExecutionHelper.Apply(new EndOfPhaseAction(actorId), world, runtimes.For(actorId));

		var state = world.StateOf(actorId);
		if (!state.IsAlive)
			return int.MinValue;

		var score = -state.ActionPoints * UnusedApPenalty;
		score += DamageAdjustment(anchor, frame.Actions, actorId, searchStartDepth);
		score += EngagementAdjustment(world, actorId, anchor, frame.Actions, searchStartDepth);
		return score;
	}

	public static int UpperBound(BattleWorld world, string actorId)
	{
		var state = world.StateOf(actorId);
		var score = FacingWeight
			+ System.Math.Max(state.ActionPoints, state.Maneuverability.MaxActionPoints) * ApproachWeight;
		var hasOffensiveCharges = HasOffensiveCharges(state);
		var goopUpperBound = state.ReadyMounts(EAbilityKind.GoopGun) > 0
			? CountBlockableOpponentThreats(world, actorId, FrozenSet<Coord>.Empty) * GoopThreatBlockBonus
			: 0;
		if (!hasOffensiveCharges && goopUpperBound == 0)
			return 0;

		if (hasOffensiveCharges)
		{
			var opponent = NearestOpponent(world, actorId);
			var weaponReach = OptimisticWeaponReach(state);
			if (weaponReach > 0
				&& opponent is not null
				&& OffensiveReach.CouldPossiblyDamage(
					state.Position,
					state.ActionPoints,
					opponent.State.Position,
					weaponReach))
				score += DamageHitBonus;
		}

		score += goopUpperBound;

		return score;
	}

	public static bool HasDamageHit(
		BattleSimulation anchor,
		IReadOnlyList<IAction> actions,
		string actorId,
		int searchStartDepth) =>
		DamageAdjustment(anchor, actions, actorId, searchStartDepth) == DamageHitBonus;

	public static bool ShouldExploreAction(
		BattleSimulation simulation,
		string actorId,
		IAction action) =>
		action is not GoopGunAction goop
		|| CountThreatsBlockedByGoop(simulation.World, actorId, goop) > 0;

	private static int EngagementAdjustment(
		BattleWorld world,
		string actorId,
		BattleSimulation anchor,
		IReadOnlyList<IAction> actions,
		int searchStartDepth)
	{
		var state = world.StateOf(actorId);
		if (!HasOffensiveCharges(state) || OffensiveFired(actions, actorId, searchStartDepth))
			return 0;

		if (CanDamageNow(world, actorId))
			return 0;

		var opponent = NearestOpponent(world, actorId);
		if (opponent is null)
			return 0;

		var bonus = 0;
		if (IsFacing(state, opponent.State.Position))
			bonus += FacingWeight;

		var startPosition = anchor.ReplayWorld(searchStartDepth).StateOf(actorId).Position;
		var distanceClosed = startPosition.ManhattanDistanceTo(opponent.State.Position)
			- state.Position.ManhattanDistanceTo(opponent.State.Position);
		if (distanceClosed > 0)
			bonus += distanceClosed * ApproachWeight;

		return bonus;
	}

	private static bool HasOffensiveCharges(State state) =>
		state.UsesRemaining(EAbilityKind.LightningCannon) > 0
		|| state.UsesRemaining(EAbilityKind.ScrapDroneSwarm) > 0;

	private static int OptimisticWeaponReach(State state)
	{
		var reach = 0;
		foreach (var installed in state.Loadout.InstalledAbilities)
		{
			if (installed.Spec is not IPerTurnAbility)
				continue;

			if (state.UsesRemaining(installed.Mount) <= 0)
				continue;

			reach = System.Math.Max(reach, AbilityReach.MaxManhattanFromFirer(installed.Spec));
		}

		return reach;
	}

	private static bool OffensiveFired(IReadOnlyList<IAction> actions, string actorId, int searchStartDepth)
	{
		for (var i = searchStartDepth; i < actions.Count; i++)
		{
			if (actions[i] is LightningCannonAction { ActorId: var lightningCannonActorId } && lightningCannonActorId == actorId)
				return true;

			if (actions[i] is ScrapDroneSwarmAction { ActorId: var swarmActorId } && swarmActorId == actorId)
				return true;
		}

		return false;
	}

	private static Unit? NearestOpponent(BattleWorld world, string actorId)
	{
		var units = UnitRegistry.For(world);
		var actor = units.UnitOf(actorId);
		Unit? nearest = null;
		var bestDistance = int.MaxValue;

		foreach (var unit in units.Except(actorId))
		{
			if (!unit.State.IsAlive || actor.RelationTo(unit) != EUnitRelation.Opponent)
				continue;

			var distance = actor.State.Position.ManhattanDistanceTo(unit.State.Position);
			if (distance >= bestDistance)
				continue;

			bestDistance = distance;
			nearest = unit;
		}

		return nearest;
	}

	private static bool IsFacing(State actor, Coord target) =>
		actor.Fore == AxisToward(actor.Position, target);

	private static Coord AxisToward(Coord from, Coord to)
	{
		var delta = to - from;
		var ax = System.Math.Abs(delta.X);
		var ay = System.Math.Abs(delta.Y);
		var az = System.Math.Abs(delta.Z);

		if (ax >= ay && ax >= az)
			return new Coord(System.Math.Sign(delta.X), 0, 0);

		if (ay >= ax && ay >= az)
			return new Coord(0, System.Math.Sign(delta.Y), 0);

		return new Coord(0, 0, System.Math.Sign(delta.Z));
	}

	private static int DamageAdjustment(
		BattleSimulation anchor,
		IReadOnlyList<IAction> actions,
		string actorId,
		int searchStartDepth)
	{
		var adjustment = 0;
		for (var i = searchStartDepth; i < actions.Count; i++)
		{
			var action = actions[i];
			if (action.ActorId != actorId)
				continue;

			adjustment += action switch
			{
				LightningCannonAction or ScrapDroneSwarmAction =>
					WouldDamage(anchor, actions, actorId, i, searchStartDepth)
						? DamageHitBonus
						: -DamageHitBonus,
				GoopGunAction goop =>
					GoopThreatBlockAdjustment(anchor, actions, actorId, searchStartDepth, i, goop),
				_ => 0,
			};
		}

		return adjustment;
	}

	private static int GoopThreatBlockAdjustment(
		BattleSimulation anchor,
		IReadOnlyList<IAction> actions,
		string actorId,
		int searchStartDepth,
		int actionIndex,
		GoopGunAction goop)
	{
		var world = WorldAtAction(anchor, actions, searchStartDepth, actionIndex);
		var blocked = CountThreatsBlockedByGoop(world, actorId, goop);
		return blocked * GoopThreatBlockBonus;
	}

	private static int CountThreatsBlockedByGoop(
		BattleWorld world,
		string actorId,
		GoopGunAction goop)
	{
		var goopCells = GoopGunDef.Instance.AffectedCells(goop, world);
		if (goopCells.Count == 0)
			return 0;

		var threatsBefore = CountBlockableOpponentThreats(world, actorId, world.AbilityBlockingCells);
		if (threatsBefore == 0)
			return 0;

		var blockingWithGoop = WithAdditionalBlocking(world.AbilityBlockingCells, goopCells);
		var threatsAfter = CountBlockableOpponentThreats(world, actorId, blockingWithGoop);
		var blocked = threatsBefore - threatsAfter;
		return System.Math.Max(blocked, 0);
	}

	private static bool WouldDamage(
		BattleSimulation anchor,
		IReadOnlyList<IAction> actions,
		string actorId,
		int actionIndex,
		int searchStartDepth)
	{
		var world = WorldAtAction(anchor, actions, searchStartDepth, actionIndex);
		return WouldDamage(world, actorId, actions[actionIndex]);
	}

	private static bool WouldDamage(BattleWorld world, string actorId, IAction action) =>
		action switch
		{
			LightningCannonAction { ActorId: var lightningCannonActorId, MountedOn: var mountedOn } when lightningCannonActorId == actorId =>
				WouldLightningCannonDamage(world, actorId, mountedOn),
			ScrapDroneSwarmAction { ActorId: var swarmActorId, MountedOn: var mountedOn } when swarmActorId == actorId =>
				WouldScrapDroneSwarmDamage(world, actorId, mountedOn),
			_ => false,
		};

	private static bool CanDamageNow(BattleWorld world, string actorId)
	{
		var state = world.StateOf(actorId);
		foreach (var installed in state.Loadout.InstalledAbilities)
		{
			if (state.UsesRemaining(installed.Mount) <= 0)
				continue;

			if (installed.Spec.Kind == EAbilityKind.LightningCannon
				&& WouldLightningCannonDamage(world, actorId, installed.MountedOn))
				return true;

			if (installed.Spec.Kind == EAbilityKind.ScrapDroneSwarm
				&& WouldScrapDroneSwarmDamage(world, actorId, installed.MountedOn))
				return true;
		}

		return false;
	}

	private static bool WouldLightningCannonDamage(
		BattleWorld world,
		string actorId,
		ESpatialOrientation mountedOn = ESpatialOrientation.Forward)
	{
		var cells = LightningCannonDef.Instance.AffectedCells(new LightningCannonAction(actorId, mountedOn), world);
		return world.AnyOpponentInCells(actorId, cells);
	}

	private static bool WouldScrapDroneSwarmDamage(BattleWorld world, string actorId, ESpatialOrientation mountedOn)
	{
		var cells = ScrapDroneSwarmDef.Instance.AffectedCells(new ScrapDroneSwarmAction(actorId, mountedOn), world);
		return world.AnyOpponentInCells(actorId, cells);
	}

	// Counts threats against the actor and its allies, since a goop shot that shields a
	// squadmate is just as valuable as one that shields the shooter.
	private static int CountBlockableOpponentThreats(
		BattleWorld world,
		string actorId,
		FrozenSet<Coord> blockingCells)
	{
		var units = UnitRegistry.For(world);
		var actor = units.UnitOf(actorId);
		var squadTargets = new List<Coord>();

		foreach (var unit in units.All)
		{
			if (unit.State.IsAlive && actor.RelationTo(unit) != EUnitRelation.Opponent)
				squadTargets.Add(unit.State.Position);
		}

		var threats = 0;
		foreach (var unit in units.Except(actorId))
		{
			if (!unit.State.IsAlive || actor.RelationTo(unit) != EUnitRelation.Opponent)
				continue;

			threats += CountBlockableThreatsFromAttacker(world, unit.State, squadTargets, blockingCells);
		}

		return threats;
	}

	// A weapon's affected cells are independent of who's standing in them, so they're
	// computed once per attacker ability and tested against every potential target.
	private static int CountBlockableThreatsFromAttacker(
		BattleWorld world,
		State attacker,
		IReadOnlyList<Coord> targets,
		FrozenSet<Coord> blockingCells)
	{
		var threats = 0;
		foreach (var installed in attacker.Loadout.InstalledAbilities)
		{
			if (!MountCanFireNow(attacker, installed))
				continue;

			if (installed.Spec is not IAreaDamage { Damage: > 0 })
				continue;

			var cells = BlockableWeaponCells(world, attacker.Id, installed.MountedOn, installed.Spec, blockingCells);
			foreach (var target in targets)
			{
				if (cells.Contains(target))
					threats++;
			}
		}

		return threats;
	}

	private static bool MountCanFireNow(State state, InstalledAbility installed)
	{
		if (installed.Spec is IPerTurnAbility)
			return state.UsesRemaining(installed.Mount) > 0;

		if (installed.Spec is ICooldownAbility)
			return state.MountRuntimeFor(installed.Mount).CooldownRemaining == 0;

		return false;
	}

	private static FrozenSet<Coord> BlockableWeaponCells(
		BattleWorld world,
		string actorId,
		ESpatialOrientation mountedOn,
		AbilitySpec spec,
		FrozenSet<Coord> blockingCells)
	{
		if (spec is not IAreaDamage areaDamage)
			return FrozenSet<Coord>.Empty;

		var state = world.StateOf(actorId);
		var frame = BodyFrame.From(state);
		var geometric = AbilityArea.CellsInBounds(areaDamage, frame, mountedOn, world.Grid);
		var filtered = AbilityArea.ApplyBlocking(
			state.Position,
			geometric,
			IsBlockableKind(spec.Kind),
			blockingCells);
		return filtered is FrozenSet<Coord> frozen ? frozen : filtered.ToFrozenSet();
	}

	private static bool IsBlockableKind(EAbilityKind kind) =>
		kind switch
		{
			EAbilityKind.LightningCannon => LightningCannonDef.Instance.IsBlockable,
			EAbilityKind.ScrapDroneSwarm => ScrapDroneSwarmDef.Instance.IsBlockable,
			EAbilityKind.GoopGun => GoopGunDef.Instance.IsBlockable,
			_ => false,
		};

	private static FrozenSet<Coord> WithAdditionalBlocking(
		FrozenSet<Coord> blockingCells,
		IReadOnlySet<Coord> additionalCells)
	{
		if (additionalCells.Count == 0)
			return blockingCells;

		var merged = blockingCells.Count == 0
			? new HashSet<Coord>()
			: blockingCells.ToHashSet();
		merged.UnionWith(additionalCells);
		return merged.ToFrozenSet();
	}

	private static BattleWorld WorldAtAction(
		BattleSimulation anchor,
		IReadOnlyList<IAction> actions,
		int searchStartDepth,
		int actionIndex)
	{
		var world = anchor.ReplayWorld(searchStartDepth);
		var runtimes = anchor.Runtimes.Fork();
		for (var i = searchStartDepth; i < actionIndex; i++)
			ExecutionHelper.Apply(actions[i], world, runtimes.For(actions[i]));

		return world;
	}
}

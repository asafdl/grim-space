using System.Collections.Concurrent;
using System.Collections.Frozen;
using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Dfs;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Battle.Ai;

internal static class EnemySearchInput
{
	internal const int UnusedApPenalty = 100;
	internal const int DamageHitBonus = 2_000;
	internal const int VoidBombAlignBonus = DamageHitBonus;
	internal const int GoopThreatBlockBonus = 2_500;
	internal const int FacingWeight = 800;
	internal const int ApproachWeight = 150;

	public static int ScoreHeuristic(
		SearchFrame<BattleWorld, ActorRuntime> frame,
		BattleSimulation anchor,
		string actorId,
		int searchStartDepth)
	{
		var world = frame.World;

		var state = world.StateOf(actorId);
		if (!state.IsAlive)
			return int.MinValue;

		var score = -state.CurrentActionPoints * UnusedApPenalty;
		score += DamageAdjustment(anchor, frame.Actions, actorId, searchStartDepth);
		score += EngagementAdjustment(world, actorId, anchor, frame.Actions, searchStartDepth);
		return score;
	}

	// Bounds every descendant's ScoreHeuristic: what the path already earned plus what the
	// remaining turn could add.
	public static int UpperBound(
		SearchFrame<BattleWorld, ActorRuntime> frame,
		BattleSimulation anchor,
		string actorId,
		int searchStartDepth)
	{
		var earned = DamageAdjustment(anchor, frame.Actions, actorId, searchStartDepth);
		return earned + RemainingUpperBound(
			frame.World, anchor, actorId, frame.Actions, searchStartDepth);
	}

	internal static Func<Simulation<BattleWorld, ActorRuntime>, SearchEvaluation> CreateEvaluator(
		BattleSimulation anchor,
		string actorId)
	{
		var threats = ThreatAreas(anchor.World, actorId);
		var reachCache = new ConcurrentDictionary<(Coord Position, int Budget), Coord[]>();
		IReadOnlyList<Coord>? Reach(Coord position, int budget) =>
			CanReachWell(anchor.World, position, budget)
				? reachCache.GetOrAdd((position, budget),
					key => OffensiveReach.RelaxedReach(anchor.World, key.Position, key.Budget))
				: null;
		return simulation => Evaluate(simulation, anchor, actorId, threats, Reach);
	}

	internal static SearchEvaluation Evaluate(
		Simulation<BattleWorld, ActorRuntime> simulation,
		BattleSimulation anchor,
		string actorId,
		IReadOnlyList<FrozenSet<Coord>>? threats = null,
		Func<Coord, int, IReadOnlyList<Coord>?>? reach = null)
	{
		var world = simulation.World;
		var state = world.StateOf(actorId);
		if (!state.IsAlive)
			return new SearchEvaluation(int.MinValue, int.MinValue);

		var start = anchor.Actions.Count;
		var earned = DamageAdjustment(anchor, simulation.Actions, actorId, start);
		var score = earned - state.CurrentActionPoints * UnusedApPenalty
			+ EngagementAdjustment(world, actorId, anchor, simulation.Actions, start);
		return new SearchEvaluation(score, earned + RemainingUpperBound(
			world, anchor, actorId, simulation.Actions, start, threats, reach));
	}

	private static int RemainingUpperBound(
		BattleWorld world,
		BattleSimulation anchor,
		string actorId,
		IReadOnlyList<IAction> actions,
		int start,
		IReadOnlyList<FrozenSet<Coord>>? threatAreas = null,
		Func<Coord, int, IReadOnlyList<Coord>?>? reach = null)
	{
		var state = world.StateOf(actorId);
		var remainingDepth = System.Math.Max(0, SearchBridge.MaxSearchDepth - (actions.Count - start));
		if (remainingDepth == 0)
			return -state.CurrentActionPoints * UnusedApPenalty
				+ EngagementAdjustment(world, actorId, anchor, actions, start);

		var movement = state.CurrentActionPoints;
		if (state.OverchargeEnginesCooldownRemaining == 0
			&& anchor.RuntimeFor(actorId).PilotCapabilities.Contains(EPilotCapabilityId.OverchargeEngines))
			movement += PilotSkillPercents.OverchargeEngines.RefreshedAp;
		var shots = 0;
		var goopShots = 0;
		var recoil = 0;
		var ramMovement = 0;
		var ramReach = 0;
		var weaponReach = OptimisticWeaponReach(state);
		var canSpawn = false;
		var voidBombBonus = 0;
		foreach (var installed in state.Loadout.InstalledAbilities)
		{
			if (installed.Spec.Kind is EAbilityKind.LightningCannon or EAbilityKind.ScrapDroneSwarm or EAbilityKind.Ram)
				shots += state.UsesRemaining(installed.Mount);
			if (installed.Spec is RamSpec ram)
			{
				ramMovement += ram.Range * state.UsesRemaining(installed.Mount);
				if (state.UsesRemaining(installed.Mount) > 0)
					ramReach = System.Math.Max(ramReach, ram.Range);
			}
			if (installed.Spec is RoadsWeDontNeedRoadsSpec repeatJump
				&& ((ICooldownAbility)repeatJump).ResetsCooldownOnKill)
				movement += remainingDepth * repeatJump.Range;
			if (!MountCanFireNow(state, installed))
				continue;
			if (installed.Spec is GoopGunSpec goop)
			{
				recoil += goop.RecoilCells;
				goopShots++;
			}
			if (installed.Spec is RoadsWeDontNeedRoadsSpec jump)
				movement += jump.Range;
			if (installed.Spec.Kind != EAbilityKind.VoidBombLauncher)
				continue;
			canSpawn = true;
			if (VoidBombPointsAtOpponent(world, actorId, installed.MountedOn))
				voidBombBonus += VoidBombAlignBonus;
		}
		IReadOnlyList<Coord>? Reach(int budget) => reach is not null
			? reach(state.Position, budget)
			: CanReachWell(world, state.Position, budget)
				? OffensiveReach.RelaxedReach(world, state.Position, budget)
				: null;
		var cells = Reach(movement);
		var radius = cells?.Max(cell => state.Position.ManhattanDistanceTo(cell)) ?? movement;

		var actor = world.UnitRegistry.UnitOf(actorId);
		var areas = goopShots > 0 ? threatAreas ?? ThreatAreas(world, actorId) : [];
		var ramPossible = ramReach > 0 && world.UnitRegistry.All.Any(opponent =>
			opponent.State.IsAlive && actor.RelationTo(opponent) == EUnitRelation.Opponent
			&& state.Position.ManhattanDistanceTo(opponent.State.Position) <= radius + ramReach);
		var threatMovement = radius + (ramPossible ? ramMovement : 0);
		if (goopShots > 0 && PotentialThreats(world, actorId, areas, threatMovement, canSpawn, remainingDepth) > 0)
		{
			movement += recoil;
			cells = Reach(movement);
			radius = cells?.Max(cell => state.Position.ManhattanDistanceTo(cell)) ?? movement;
			threatMovement = radius + ramMovement;
		}
		var engagement = 0;
		var canHit = false;
		var startPosition = anchor.World.StateOf(actorId).Position;
		foreach (var opponent in world.UnitRegistry.All)
		{
			if (!opponent.State.IsAlive || actor.RelationTo(opponent) != EUnitRelation.Opponent)
				continue;
			var distance = cells?.Min(cell => cell.ManhattanDistanceTo(opponent.State.Position))
				?? System.Math.Max(0, state.Position.ManhattanDistanceTo(opponent.State.Position) - movement);
			canHit |= distance <= weaponReach;
			engagement = System.Math.Max(engagement,
				startPosition.ManhattanDistanceTo(opponent.State.Position) - distance);
		}

		var engagementBound = HasOffensiveCharges(state) && !OffensiveFired(actions, actorId, start)
			? FacingWeight + System.Math.Max(0, engagement) * ApproachWeight
			: 0;
		var damageBound = canHit ? System.Math.Min(shots, remainingDepth) * DamageHitBonus : 0;
		var threats = PotentialThreats(world, actorId, areas, threatMovement, canSpawn, remainingDepth);
		var goopBound = System.Math.Min(goopShots, remainingDepth) * threats * GoopThreatBlockBonus;
		// Taking an offensive shot disables engagement scoring; descendants cannot earn both.
		return System.Math.Max(engagementBound, damageBound) + goopBound + voidBombBonus;
	}

	private static bool CanReachWell(BattleWorld world, Coord position, int budget) =>
		world.Hazards.OfType<GravityWell>().Any(well =>
			well.AssistCells.Any(cell => position.ManhattanDistanceTo(cell) <= budget));

	private static int PotentialThreats(
		BattleWorld world, string actorId, IReadOnlyList<FrozenSet<Coord>> areas,
		int movement, bool canSpawn, int remainingDepth)
	{
		var actor = world.UnitRegistry.UnitOf(actorId);
		var threats = 0;
		foreach (var area in areas)
		{
			threats += world.UnitRegistry.All.Count(unit =>
				unit.State.Id != actorId && unit.State.IsAlive
				&& actor.RelationTo(unit) != EUnitRelation.Opponent
				&& area.Contains(unit.State.Position));
			if (area.Any(cell => actor.State.Position.ManhattanDistanceTo(cell) <= movement))
				threats++;
			if (canSpawn && area.Any(cell => actor.State.Position.ManhattanDistanceTo(cell) <= movement + 1))
				threats += remainingDepth;
		}
		return threats;
	}

	private static IReadOnlyList<FrozenSet<Coord>> ThreatAreas(BattleWorld world, string actorId)
	{
		var areas = new List<FrozenSet<Coord>>();
		var actor = world.UnitRegistry.UnitOf(actorId);
		foreach (var opponent in world.UnitRegistry.All)
		{
			if (!opponent.State.IsAlive || actor.RelationTo(opponent) != EUnitRelation.Opponent)
				continue;
			foreach (var installed in opponent.State.Loadout.InstalledAbilities)
			{
				if (MountCanFireNow(opponent.State, installed)
					&& IsBlockableKind(installed.Spec.Kind)
					&& installed.Spec is IAreaDamage { Damage: > 0 })
					areas.Add(BlockableWeaponCells(
						world, opponent.State.Id, installed.MountedOn, installed.Spec, FrozenSet<Coord>.Empty));
			}
		}
		return areas;
	}

	public static bool ShouldExploreAction(
		BattleSimulation simulation,
		string actorId,
		IAction action)
	{
		if (action is GoopGunAction goop)
			return CountThreatsBlockedByGoop(simulation.World, actorId, goop) > 0;
		if (action is VoidBombAction bomb)
			return VoidBombPointsAtOpponent(simulation.World, actorId, bomb.MountedOn);
		return true;
	}

	public static ProtectionDroneAction? BestProtectionDroneAction(
		BattleWorld world,
		ActorRuntime runtime,
		string actorId)
	{
		ProtectionDroneAction? best = null;
		var bestThreats = 0;
		foreach (var action in ProtectionDroneDef.Instance.Discover(world, runtime, actorId))
		{
			if (action is not ProtectionDroneAction drone)
				continue;

			var threats = CountThreatsAgainst(world, actorId, drone.TargetId);
			if (threats <= bestThreats)
				continue;

			bestThreats = threats;
			best = drone;
		}

		return bestThreats > 0 ? best : null;
	}

	internal static int CountThreatsAgainst(BattleWorld world, string actorId, string targetId)
	{
		var units = UnitRegistry.For(world);
		var actor = units.UnitOf(actorId);
		var targets = new[] { world.StateOf(targetId).Position };
		var threats = 0;
		foreach (var unit in units.Except(actorId))
		{
			if (!unit.State.IsAlive || actor.RelationTo(unit) != EUnitRelation.Opponent)
				continue;

			threats += CountBlockableThreatsFromAttacker(
				world,
				unit.State,
				targets,
				world.AbilityBlockingCells);
		}

		return threats;
	}

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

		var startPosition = anchor.World.StateOf(actorId).Position;
		var distanceClosed = startPosition.ManhattanDistanceTo(opponent.State.Position)
			- state.Position.ManhattanDistanceTo(opponent.State.Position);
		if (distanceClosed > 0)
			bonus += distanceClosed * ApproachWeight;

		return bonus;
	}

	private static bool HasOffensiveCharges(State state) =>
		state.UsesRemaining(EAbilityKind.LightningCannon) > 0
		|| state.UsesRemaining(EAbilityKind.ScrapDroneSwarm) > 0
		|| state.UsesRemaining(EAbilityKind.Ram) > 0;

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

			if (actions[i] is RamAction { ActorId: var ramActorId } && ramActorId == actorId)
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
				LightningCannonAction or ScrapDroneSwarmAction or RamAction =>
					WouldDamage(anchor, actions, actorId, i, searchStartDepth)
						? DamageHitBonus
						: -DamageHitBonus,
				GoopGunAction goop =>
					GoopThreatBlockAdjustment(anchor, actions, actorId, searchStartDepth, i, goop),
				VoidBombAction { ActorId: var bombActorId, MountedOn: var mountedOn } when bombActorId == actorId =>
					VoidBombPointsAtOpponent(WorldAtAction(anchor, actions, searchStartDepth, i), actorId, mountedOn)
						? VoidBombAlignBonus
						: -VoidBombAlignBonus,
				_ => 0,
			};
		}

		return adjustment;
	}

	internal static bool VoidBombPointsAtOpponent(
		BattleWorld world,
		string actorId,
		ESpatialOrientation mountedOn)
	{
		var state = world.StateOf(actorId);
		if (state.FindInstalled(EAbilityKind.VoidBombLauncher, mountedOn)?.Spec is not VoidBombLauncherSpec launcher)
			return false;

		var (origin, fore, _) = VoidBombMount.LaunchPose(state, mountedOn);
		var reach = launcher.FuelTurns * (launcher.MovementActionPoints / launcher.ForwardMoveApCost)
			+ launcher.BlastRadius;
		var units = UnitRegistry.For(world);
		var actor = units.UnitOf(actorId);
		foreach (var unit in units.Except(actorId))
		{
			if (!unit.State.IsAlive || actor.RelationTo(unit) != EUnitRelation.Opponent)
				continue;

			var offset = unit.State.Position - origin;
			var along = Coord.Dot(offset, fore);
			if (along <= 0 || along > reach)
				continue;
			if (offset.ManhattanDistanceTo(Coord.Zero) - along <= launcher.BlastRadius)
				return true;
		}

		return false;
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
			RamAction { ActorId: var ramActorId } when ramActorId == actorId =>
				WouldRamDamage(world, actorId),
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

			if (installed.Spec.Kind == EAbilityKind.Ram
				&& WouldRamDamage(world, actorId))
				return true;
		}

		return false;
	}

	private static bool WouldLightningCannonDamage(
		BattleWorld world,
		string actorId,
		ESpatialOrientation mountedOn = ESpatialOrientation.Forward)
	{
		var state = world.StateOf(actorId);
		var installed = state.FindInstalled(EAbilityKind.LightningCannon, mountedOn);
		if (installed?.Spec is not LightningCannonSpec lightningCannon)
			return false;

		return ChainLightning.WouldDamageOpponent(world, actorId, lightningCannon, mountedOn);
	}

	private static bool WouldScrapDroneSwarmDamage(BattleWorld world, string actorId, ESpatialOrientation mountedOn)
	{
		var cells = ScrapDroneSwarmDef.Instance.AffectedCells(new ScrapDroneSwarmAction(actorId, mountedOn), world);
		return world.AnyOpponentInCells(actorId, cells);
	}

	private static bool WouldRamDamage(BattleWorld world, string actorId) =>
		RamDef.TryFindTarget(world, actorId, out _, out _);

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

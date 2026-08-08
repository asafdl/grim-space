using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Weapons;
using GrimSpace.Battle.World;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Presentation.Domains.Torpedo;

public static class TorpedoUi
{
	private static string? _envelopeCacheKey;
	private static IReadOnlyList<IReadOnlySet<Coord>> _envelopeCache = [];

	public static bool TryApply(
		BattleOrchestrator battle,
		Interaction.InteractionState state,
		Coord cell)
	{
		var actor = battle.GetActiveUnit();
		if (actor is null || actor.State.Id != battle.PlayerId || !battle.CanAct(actor))
			return false;

		if (MountForCell(battle, cell) is not { } mount)
			return false;

		var def = TorpedoDef.For(mount);
		var probe = new TorpedoAction(battle.PlayerId, mount);
		if (!def.IsLegal(probe, battle.Sim.World, battle.Sim.RuntimeFor(battle.PlayerId)))
			return false;

		var spawnedId = battle.Sim.World.IdRegistry.NextUnitId(EType.Torpedo);
		var action = new TorpedoAction(battle.PlayerId, mount, spawnedId);
		if (!battle.Sim.TryEnqueue(action))
			return false;

		state.SetMode(EPlayerMode.Move);
		return true;
	}

	public static HashSet<Coord> GetMountCells(BattleOrchestrator battle)
	{
		if (battle.GetActiveUnit() is not { State.Id: var id } || id != battle.PlayerId)
			return [];

		var sim = battle.Sim;
		var actorId = battle.PlayerId;
		var ship = sim.StateOf<ActorState>(actorId);
		var cells = new HashSet<Coord>();

		foreach (var mount in TorpedoConfig.EnabledMounts)
		{
			var action = new TorpedoAction(actorId, mount);
			if (!TorpedoDef.For(mount).IsLegal(action, sim.World, sim.RuntimeFor(actorId)))
				continue;

			var (position, _, _) = TorpedoMount.LaunchPose(ship, mount);
			cells.Add(position);
		}

		return cells;
	}

	public static IReadOnlyList<IReadOnlySet<Coord>> GetEnvelopeLayers(
		BattleOrchestrator battle,
		Interaction.InteractionState state)
	{
		if (state.TorpedoHover is not Coord hover)
			return [];

		if (MountForCell(battle, hover) is not { } mount)
			return [];

		var sim = battle.Sim;
		var cacheKey =
			$"{sim.WorldVersion}|{MovePreviewCache.PrefixKey(sim.Actions)}|{mount}";
		if (_envelopeCacheKey == cacheKey)
			return _envelopeCache;

		var spawnedId = sim.World.IdRegistry.NextUnitId(EType.Torpedo);
		var peek = sim.Peek(new TorpedoAction(battle.PlayerId, mount, spawnedId));
		if (peek is null
			|| !UnitRegistry.For(peek.Value.World).TryGet(spawnedId, out var spawned))
		{
			_envelopeCacheKey = cacheKey;
			_envelopeCache = [];
			return _envelopeCache;
		}

		var session = new BattleSimulation(peek.Value.World, peek.Value.Runtimes);
		session.Begin(sim.AnchorTick, sim.WorldVersion);
		var envelope = TorpedoReachEnvelope.Build(session, spawned.State.Id);

		_envelopeCacheKey = cacheKey;
		_envelopeCache = envelope.Layers;
		return _envelopeCache;
	}

	public static ETorpedoMount? MountForCell(BattleOrchestrator battle, Coord cell)
	{
		var ship = battle.Sim.StateOf<ActorState>(battle.PlayerId);
		foreach (var mount in TorpedoConfig.EnabledMounts)
		{
			var (position, _, _) = TorpedoMount.LaunchPose(ship, mount);
			if (position == cell)
				return mount;
		}

		return null;
	}
}

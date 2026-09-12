using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Presentation.Interaction;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Presentation;

/// <summary>
/// Sim-derived planning previews for presentation. Hover-sensitive fields peek the planning sim locally.
/// </summary>
public sealed class PlanningPreview
{
	private readonly MovePreviewCache _moveCache = new();

	private BattleSimulation? _lastSim;

	public IReadOnlyDictionary<string, UnitDisplayState> PreviewUnits(
		BattleSimulation sim,
		string playerId) =>
		CaptureUnits(PreviewWorld(sim, playerId));

	public IReadOnlyList<MovePathOption> MoveOptions(
		BattleSimulation sim,
		string playerId,
		string focusId,
		bool isPlanning)
	{
		if (!isPlanning)
			return [];

		EnsureSim(sim);
		var moveActorId = focusId;
		var inspecting = moveActorId != playerId;
		var previewWorld = PreviewWorld(sim, playerId);

		if (moveActorId == playerId)
		{
			return _moveCache.GetPaths(sim, playerId, sim.Actions)
				.Select(path => new MovePathOption(
					path.Steps,
					path.Checkpoints,
					path.EndPosition,
					path.EndBasis,
					path.ExtensionApCost,
					path.RemainingAp,
					UnitDisplayState.Capture(path.ResultState)))
				.ToList();
		}

		if (!inspecting || !CaptureUnits(previewWorld).ContainsKey(moveActorId))
			return [];

		return MovePathEndpoints.DiscoverExtensions(sim, moveActorId)
			.Select(path => new MovePathOption(
				path.Steps,
				path.Checkpoints,
				path.EndPosition,
				path.EndBasis,
				path.ExtensionApCost,
				path.RemainingAp,
				UnitDisplayState.Capture(path.ResultState)))
			.ToList();
	}

	public int MovePathApBaseline(BattleSimulation sim, string playerId, string focusId) =>
		0;

	public IReadOnlyList<MoveCheckpoint> CommittedMoveCheckpoints(BattleSimulation sim, string playerId) =>
		CommittedMoveCheckpoints(sim.ReplayWorld(0).StateOf(playerId), sim.Actions, playerId);

	private static IReadOnlyList<MoveCheckpoint> CommittedMoveCheckpoints(
		State start,
		IReadOnlyList<IAction> actions,
		string playerId)
	{
		var position = start.Position;
		var basis = GridBasis.From(start.Fore, start.Dorsal, start.Starboard);
		var checkpoints = new List<MoveCheckpoint>();
		foreach (var action in actions)
		{
			if (action is not MoveStepAction { ActorId: var actorId } move || actorId != playerId)
				continue;

			var transition = Orientation.MoveStep(position, basis, move.Heading, move.Roll);
			position = transition.Destination;
			basis = transition.ArrivalBasis;
			checkpoints.Add(new MoveCheckpoint(position, basis));
		}

		return checkpoints;
	}

	public WeaponPeek Weapons(BattleSimulation sim, string actorId)
		=> Weapons(Capabilities.LegalCapabilities(sim, actorId));

	public AbilityLegality Abilities(BattleSimulation sim, string actorId)
	{
		var actions = Capabilities.LegalCapabilities(sim, actorId);
		return new AbilityLegality(
			Weapons(actions),
			actions.Any(action => action is SpawnPatrolAction),
			actions.Any(action => action is DetonateAction));
	}

	private static WeaponPeek Weapons(IReadOnlyList<IAction> actions)
	{
		var portFlak = false;
		var starboardFlak = false;
		var railgun = false;
		var torpedoMounts = new HashSet<ESpatialOrientation>();

		foreach (var action in actions)
		{
			switch (action)
			{
				case FlakAction { MountedOn: ESpatialOrientation.Port }:
					portFlak = true;
					break;
				case FlakAction { MountedOn: ESpatialOrientation.Starboard }:
					starboardFlak = true;
					break;
				case RailgunAction:
					railgun = true;
					break;
				case TorpedoAction torpedo:
					torpedoMounts.Add(torpedo.MountedOn);
					break;
			}
		}

		return new WeaponPeek(portFlak, starboardFlak, railgun, torpedoMounts);
	}

	public QueuedWeaponState QueuedWeapon(BattleSimulation sim, string playerId)
	{
		ESpatialOrientation? flakMountedOn = null;
		UnitDisplayState? flakActorState = null;
		var railgun = false;
		UnitDisplayState? railgunActorState = null;
		ESpatialOrientation? torpedoMountedOn = null;
		UnitDisplayState? torpedoActorState = null;

		for (var i = sim.Actions.Count - 1; i >= 0; i--)
		{
			if (sim.Actions[i].ActorId != playerId)
				continue;

			switch (sim.Actions[i])
			{
				case FlakAction flak when flakMountedOn is null:
					flakMountedOn = flak.MountedOn;
					flakActorState = ActorStateAt(sim, playerId, i);
					break;
				case RailgunAction when !railgun:
					railgun = true;
					railgunActorState = ActorStateAt(sim, playerId, i);
					break;
				case TorpedoAction torpedo when torpedoMountedOn is null:
					torpedoMountedOn = torpedo.MountedOn;
					torpedoActorState = ActorStateAt(sim, playerId, i);
					break;
			}
		}

		return new QueuedWeaponState
		{
			FlakMountedOn = flakMountedOn,
			FlakActorStateAtQueue = flakActorState,
			Railgun = railgun,
			RailgunActorStateAtQueue = railgunActorState,
			TorpedoMountedOn = torpedoMountedOn,
			TorpedoActorStateAtQueue = torpedoActorState,
		};
	}

	public HashSet<string> ThreatenedUnitIds(
		BattleSimulation sim,
		string playerId,
		InteractionState state)
	{
		var targets = new HashSet<string>();

		if (state.Mode == EPlayerMode.Flak)
		{
			if (state.StagedMountedOn is ESpatialOrientation stagedMountedOn)
				targets.UnionWith(ImpactTargets(sim.Peek(new FlakAction(playerId, stagedMountedOn))));
			else if (state.FlakHoverMountedOn is ESpatialOrientation hoverMountedOn)
				targets.UnionWith(ImpactTargets(sim.Peek(new FlakAction(playerId, hoverMountedOn))));
		}

		if (state.RailgunHovered)
			targets.UnionWith(ImpactTargets(sim.Peek(new RailgunAction(playerId))));

		for (var i = 0; i < sim.Actions.Count; i++)
		{
			if (sim.Actions[i].ActorId != playerId)
				continue;

			switch (sim.Actions[i])
			{
				case FlakAction:
				case RailgunAction:
					targets.UnionWith(ImpactTargets(sim.RecordsFor(i)));
					break;
			}
		}

		return targets;
	}

	private void EnsureSim(BattleSimulation sim)
	{
		if (ReferenceEquals(sim, _lastSim))
			return;

		ClearCaches();
		_lastSim = sim;
	}

	public void ClearCaches()
	{
		_moveCache.Clear();
		_lastSim = null;
	}

	private static BattleWorld PreviewWorld(BattleSimulation sim, string playerId)
	{
		var peek = sim.Peek(EndOfPhaseDef.Instance.Bind(playerId));
		return peek?.World ?? sim.World;
	}

	private static Dictionary<string, UnitDisplayState> CaptureUnits(BattleWorld world) =>
		UnitRegistry.For(world).All
			.ToDictionary(unit => unit.State.Id, unit => UnitDisplayState.Capture(unit.State));

	private static UnitDisplayState ActorStateAt(BattleSimulation sim, string playerId, int actionIndex)
	{
		var world = sim.ReplayWorld(actionIndex);
		return UnitDisplayState.Capture(UnitRegistry.For(world).UnitOf(playerId).State);
	}

	private static HashSet<string> ImpactTargets(PeekFrame<BattleWorld, ActorRuntime>? peek) =>
		peek is { } frame ? ImpactTargets(frame.Records) : [];

	private static HashSet<string> ImpactTargets(IReadOnlyList<IRecord> records)
	{
		var targets = new HashSet<string>();
		foreach (var record in records)
		{
			if (record is Record<ImpactFacts> { Value.TargetId: var targetId })
				targets.Add(targetId);
		}

		return targets;
	}
}

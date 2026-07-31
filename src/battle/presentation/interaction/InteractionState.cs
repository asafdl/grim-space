using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Weapons;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Interaction;

public sealed class InteractionState
{
	public EPlayerMode Mode { get; set; } = EPlayerMode.Move;
	public EMissileMount? MissileMount { get; set; }
	public int MissileRange { get; set; } = CombatConfig.ForeMissileMinRange;
	public Coord? MissileHover { get; set; }
	public Coord? FlakHover { get; set; }
	public Unit? RailgunHover { get; set; }
	public int? MoveHoveredIndex { get; set; }
	public IReadOnlyList<Coord> CommittedMovePath { get; set; } = [];
	public int ActiveRingIndex { get; private set; }
	public ERingBandPreset RingBandPreset { get; private set; } = ERingBandPreset.ApMomentum;

	private HashSet<Coord>? _preserveRingEndpoints;

	private Coord _ringSnapshotActor;
	private int _ringSnapshotOptionCount;
	private int _ringSnapshotOptionsFingerprint;

	public void SyncActiveRingForSnapshot(
		Coord actorPosition,
		IReadOnlyList<Option> moveOptions,
		MovePreviewRings.MovePreviewRingTable ringTable)
	{
		var fingerprint = OptionsFingerprint(moveOptions);
		if (actorPosition == _ringSnapshotActor
			&& moveOptions.Count == _ringSnapshotOptionCount
			&& fingerprint == _ringSnapshotOptionsFingerprint)
		{
			ActiveRingIndex = ClampRingIndex(ActiveRingIndex, ringTable.RingCount);
			return;
		}

		_ringSnapshotActor = actorPosition;
		_ringSnapshotOptionCount = moveOptions.Count;
		_ringSnapshotOptionsFingerprint = fingerprint;
		_preserveRingEndpoints = null;
		ActiveRingIndex = 0;
		MoveHoveredIndex = null;
	}

	public void SetRingBandPreset(ERingBandPreset preset, HashSet<Coord>? preserveRingEndpoints)
	{
		if (RingBandPreset == preset)
			return;

		RingBandPreset = preset;
		_preserveRingEndpoints = preserveRingEndpoints;
	}

	public void RemapActiveRingAfterResort(
		MovePreviewRings.MovePreviewRingTable table,
		IReadOnlyList<Option> moveOptions)
	{
		if (_preserveRingEndpoints is not { Count: > 0 } preserve)
		{
			ActiveRingIndex = ClampRingIndex(ActiveRingIndex, table.RingCount);
			return;
		}

		_preserveRingEndpoints = null;
		for (var i = 0; i < table.RingCount; i++)
		{
			var endpoints = new HashSet<Coord>();
			foreach (var index in table.OptionIndicesOnRing(i))
				endpoints.Add(moveOptions[index].EndPosition);

			if (endpoints.SetEquals(preserve))
			{
				ActiveRingIndex = i;
				MoveHoveredIndex = null;
				return;
			}
		}

		ActiveRingIndex = 0;
		MoveHoveredIndex = null;
	}

	public void CycleActiveRing(int delta, int ringCount)
	{
		if (ringCount <= 0)
		{
			ActiveRingIndex = 0;
			MoveHoveredIndex = null;
			return;
		}

		ActiveRingIndex = (ActiveRingIndex + delta % ringCount + ringCount) % ringCount;
		MoveHoveredIndex = null;
	}

	private static int ClampRingIndex(int index, int ringCount)
	{
		if (ringCount <= 0)
			return 0;

		return System.Math.Clamp(index, 0, ringCount - 1);
	}

	private static int OptionsFingerprint(IReadOnlyList<Option> moveOptions)
	{
		var hash = new HashCode();
		hash.Add(moveOptions.Count);
		for (var i = 0; i < moveOptions.Count; i++)
		{
			hash.Add(moveOptions[i].EndPosition);
			hash.Add(moveOptions[i].ApCost);
			hash.Add(moveOptions[i].PathLength);
			hash.Add(moveOptions[i].StartMomentumLevel);
			hash.Add(moveOptions[i].EndMomentumLevel);
		}

		return hash.ToHashCode();
	}

	public void ClearInteraction()
	{
		MoveHoveredIndex = null;
		MissileHover = null;
		FlakHover = null;
		RailgunHover = null;
	}

	public void ResetAfterTurn() => SetMoveMode();

	public void SetMoveMode()
	{
		Mode = EPlayerMode.Move;
		MissileMount = null;
		ClearInteraction();
		CommittedMovePath = [];
	}

	public void SetMode(EPlayerMode mode)
	{
		Mode = mode;
		MissileMount = null;
		ClearInteraction();
	}

	public void SelectMissileMount(EMissileMount mount)
	{
		Mode = EPlayerMode.Missile;
		MissileMount = mount;
		MissileRange = CombatConfig.ForeMissileMinRange;
		ClearInteraction();
	}

	public void SelectFlakMode()
	{
		Mode = EPlayerMode.Flak;
		MissileMount = null;
		ClearInteraction();
	}

	public void SetMoveHover(int? index, int optionCount) =>
		MoveHoveredIndex = ClampIndex(index, optionCount);

	public void ClampMoveHover(int optionCount) =>
		MoveHoveredIndex = ClampIndex(MoveHoveredIndex, optionCount);

	private static int? ClampIndex(int? index, int optionCount)
	{
		if (index is not int i || i < 0 || i >= optionCount)
			return null;

		return i;
	}
}

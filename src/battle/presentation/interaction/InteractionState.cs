using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.Presentation.Domains.Move;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Interaction;

public sealed class InteractionState
{
	private readonly List<PoseHitOpportunity> _poseHitOpportunities = [];
	private readonly List<string> _pickedTargetIds = [];

	public string? FocusId { get; private set; }
	public string? RangeEnemyId { get; private set; }
	public string? TargetCandidateId { get; private set; }
	public IReadOnlyList<string> PickedTargetIds => _pickedTargetIds;
	public bool IsTargetSessionActive => ActiveAbilitySpec?.TargetPick is not null;

	public EPlayerMode Mode { get; private set; } = EPlayerMode.Move;
	public IReadOnlyList<PoseHitOpportunity> PoseHitOpportunities => _poseHitOpportunities;
	public AbilityHudCatalog.Spec? ActiveAbilitySpec { get; private set; }
	public int? AbilityHoveredIndex { get; private set; }
	public Coord? MoveHoveredCell { get; private set; }
	public Coord? MoveDestination { get; private set; }
	public GridBasis? RequestedMoveBasis { get; private set; }
	public int MoveRollQuarters { get; private set; }
	public bool IsMovePoseSelectionActive { get; private set; }
	public string? ActionError { get; private set; }

	public void FocusUnit(string unitId)
	{
		FocusId = unitId;
		SetMode(EPlayerMode.Move);
	}

	public void ClearFocus()
	{
		FocusId = null;
		SetMode(EPlayerMode.Move);
	}

	public void SetRangeEnemy(string unitId) =>
		RangeEnemyId = unitId;

	public void ClearRangeEnemy() =>
		RangeEnemyId = null;

	public void SetMode(EPlayerMode mode, AbilityHudCatalog.Spec? abilitySpec = null)
	{
		Mode = mode;
		ActiveAbilitySpec = mode == EPlayerMode.Move ? null : abilitySpec;
		ClearTargetSession();
		ActionError = null;
		ClearHovers();
		ClearMoveSelection();
	}

	public void SetTargetCandidate(string? unitId)
	{
		if (IsTargetSessionActive)
			TargetCandidateId = unitId;
	}

	public bool ConfirmTargetPick()
	{
		if (ActiveAbilitySpec?.TargetPick is not { } pick
			|| TargetCandidateId is not { } candidate
			|| !pick.CanPickMore(_pickedTargetIds.Count)
			|| _pickedTargetIds.Contains(candidate))
			return false;

		_pickedTargetIds.Add(candidate);
		TargetCandidateId = null;
		return true;
	}

	/// <summary>Pops the last pick and returns the candidate to it.</summary>
	public bool UndoTargetPick()
	{
		if (_pickedTargetIds.Count == 0)
			return false;

		var last = _pickedTargetIds[^1];
		_pickedTargetIds.RemoveAt(_pickedTargetIds.Count - 1);
		TargetCandidateId = last;
		return true;
	}

	public void PruneTargetPicks(Func<string, bool> keep) =>
		_pickedTargetIds.RemoveAll(id => !keep(id));

	private void ClearTargetSession()
	{
		TargetCandidateId = null;
		_pickedTargetIds.Clear();
	}

	public void ResetAfterTurn()
	{
		FocusId = null;
		RangeEnemyId = null;
		SetMode(EPlayerMode.Move);
	}

	public void ClearHovers()
	{
		MoveHoveredCell = null;
		AbilityHoveredIndex = null;
	}

	public void BeginMoveSelection(Coord destination, GridBasis basis)
	{
		MoveHoveredCell = null;
		MoveDestination = destination;
		RequestedMoveBasis = basis;
		MoveRollQuarters = MovePose.RollQuarters(basis);
		IsMovePoseSelectionActive = true;
		ActionError = null;
	}

	public void SetMovePose(GridBasis basis)
	{
		if (MoveDestination is null)
			return;

		RequestedMoveBasis = basis;
		MoveRollQuarters = MovePose.RollQuarters(basis);
		ActionError = null;
	}

	public void CompleteMoveSelection()
	{
		MoveDestination = null;
		RequestedMoveBasis = null;
		MoveRollQuarters = 0;
		IsMovePoseSelectionActive = false;
		ActionError = null;
	}

	public void ClearMoveSelection()
	{
		CompleteMoveSelection();
		ClearPoseHitOpportunities();
	}

	public void RefreshPoseHitOpportunities(IReadOnlyList<PoseHitOpportunity> computed)
	{
		_poseHitOpportunities.Clear();
		_poseHitOpportunities.AddRange(computed);
	}

	public void ClearPoseHitOpportunities() => _poseHitOpportunities.Clear();

	public void ReportActionFailure()
	{
		IsMovePoseSelectionActive = false;
		ActionError = BattleHudCopy.ActionUnavailable;
	}

	public void SetMoveHover(Coord? cell, IReadOnlyList<MovePathOption> options) =>
		MoveHoveredCell = ValidateMoveHover(cell, options);

	public void ValidateMoveHover(IReadOnlyList<MovePathOption> options) =>
		MoveHoveredCell = ValidateMoveHover(MoveHoveredCell, options);

	private static Coord? ValidateMoveHover(Coord? cell, IReadOnlyList<MovePathOption> options)
	{
		if (cell is not Coord coordinate)
			return null;

		foreach (var option in options)
		{
			if (option.EndPosition == coordinate && option.Steps.Count > 0)
				return coordinate;
		}

		return null;
	}

	public void SetAbilityHover(int? index, int optionCount)
	{
		var clamped = ClampIndex(index, optionCount);
		if (AbilityHoveredIndex == clamped)
			return;

		AbilityHoveredIndex = clamped;
		ActionError = null;
	}

	public void ClampAbilityHover(int optionCount) =>
		AbilityHoveredIndex = ClampIndex(AbilityHoveredIndex, optionCount);

	private static int? ClampIndex(int? index, int optionCount)
	{
		if (index is not int i || i < 0 || i >= optionCount)
			return null;

		return i;
	}
}

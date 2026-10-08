namespace GrimSpace.Run.Persistence;

public enum SaveBlockReason
{
	None,
	NoActiveRun,
	WaitingForPlayerInput,
	ResolvingAction,
	UncommittedPlayerBatch,
	ResolvingBattle,
	ReplayingBattle,
	BattleTurnInProgress,
	SceneTransition,
}

public readonly record struct SaveGateState(
	bool HasActiveRun,
	bool StrategicWaitingForPlayerInput,
	bool StrategicResolvingAction,
	bool StrategicHasUncommittedPlayerBatch,
	bool BattleResolving,
	bool BattleReplaying,
	bool SceneTransitioning,
	bool BattleTurnInProgress = false);

public static class SaveLoadPolicy
{
	public static SaveBlockReason CanSave(SaveGateState state)
	{
		if (!state.HasActiveRun)
			return SaveBlockReason.NoActiveRun;
		if (state.SceneTransitioning)
			return SaveBlockReason.SceneTransition;
		if (state.BattleResolving)
			return SaveBlockReason.ResolvingBattle;
		if (state.BattleReplaying)
			return SaveBlockReason.ReplayingBattle;
		if (state.BattleTurnInProgress)
			return SaveBlockReason.BattleTurnInProgress;
		if (state.StrategicResolvingAction)
			return SaveBlockReason.ResolvingAction;
		if (state.StrategicHasUncommittedPlayerBatch)
			return SaveBlockReason.UncommittedPlayerBatch;
		return SaveBlockReason.None;
	}

	public static bool CanSave(SaveGateState state, out SaveBlockReason reason)
	{
		reason = CanSave(state);
		return reason == SaveBlockReason.None;
	}
}

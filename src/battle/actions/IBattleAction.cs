using GrimSpace.Battle.Slices;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Actions;

public interface IBattleAction : IAction<BattleActionContext, BattleSlices>;

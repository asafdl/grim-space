using GrimSpace.Battle.Slices;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle;

namespace GrimSpace.Battle.Actions;

public interface IBattleAction : IAction<BattleActionContext, BattleSlices>;

using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle.Contexts;

namespace GrimSpace.Core.Actions.Battle;

public interface IBattleAction : IAction<BattleBoard, BattleSlices, BattlePlanContext>;

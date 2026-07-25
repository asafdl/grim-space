using GrimSpace.Battle.Actions;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Planning;

public static class BattleActionStreamliners
{
	public static readonly IReadOnlyList<IActionStreamline> All = [HeadingDef.Instance];
}

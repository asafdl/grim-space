using Godot;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Education;

namespace GrimSpace.Battle.Presentation;

public sealed partial class BattleWorldIndicators : Node3D, IWorldIndicator
{
	public void Configure(BattleLayout layout, BattleView battleView)
	{
		_ = layout ?? throw new ArgumentNullException(nameof(layout));
		_ = battleView ?? throw new ArgumentNullException(nameof(battleView));
	}

	public WorldIndicatorResult Show(string objectId) =>
		new WorldIndicatorResult.MissingTarget();
}

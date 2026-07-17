namespace GrimSpace.Core.Actions.Battle;

public sealed class BattleTurnTags
{
	public YawState Yaw { get; } = new();

	public SpinState Spin { get; } = new();

	public void Clear()
	{
		Yaw.Clear();
		Spin.Clear();
	}
}

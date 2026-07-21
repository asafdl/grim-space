namespace GrimSpace.Core.Actions;

public abstract class ActionContext<TSlice>
{
	public abstract TSlice Slices { get; }
}

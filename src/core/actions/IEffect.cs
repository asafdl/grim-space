namespace GrimSpace.Core.Actions;

public interface IEffect<in TSlice>
{
	void Apply(TSlice slices);
}

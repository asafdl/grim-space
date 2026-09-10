namespace GrimSpace.Education;

public interface IWorldIndicator
{
	WorldIndicatorResult Show(string objectId);
}

public interface IWorldIndicatorHandle : IDisposable
{
}

public abstract record WorldIndicatorResult
{
	public sealed record Shown(IWorldIndicatorHandle Handle) : WorldIndicatorResult;

	public sealed record MissingTarget : WorldIndicatorResult;

	public sealed record AmbiguousTargetId : WorldIndicatorResult;

	public sealed record TargetNotFocusable : WorldIndicatorResult;

	public sealed record Unavailable : WorldIndicatorResult;
}

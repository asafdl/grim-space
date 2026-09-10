namespace GrimSpace.Education;

public interface IWorldFocus
{
	WorldFocusResult Focus(string objectId);
}

public abstract record WorldFocusResult
{
	public sealed record Accepted : WorldFocusResult;

	public sealed record MissingTarget : WorldFocusResult;

	public sealed record AmbiguousTargetId : WorldFocusResult;

	public sealed record TargetNotFocusable : WorldFocusResult;

	public sealed record Unavailable : WorldFocusResult;
}

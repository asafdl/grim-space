namespace GrimSpace.Core.Actions;

/// <summary>
/// Queue identity for planned actions. Board-neutral; execution uses capability interfaces (e.g. <see cref="Battle.IBattleAction"/>).
/// </summary>
public interface IAction : IEnqueueable
{
	string OwnerId { get; }
}

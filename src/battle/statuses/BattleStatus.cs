namespace GrimSpace.Battle.Statuses;

public enum EStatusPolarity
{
	Buff,
	Debuff,
	Neutral,
}

public abstract class BattleStatus
{
	public abstract EStatusPolarity Polarity { get; }

	public virtual BattleStatus DeepCopy() => this;
}

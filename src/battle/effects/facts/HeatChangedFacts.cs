namespace GrimSpace.Battle.Effects;

public readonly record struct HeatChangedFacts(
	string ActorId,
	int Heat,
	bool Overdrive,
	bool Overheat);

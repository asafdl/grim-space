using GrimSpace.Battle.Units;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Tests;

internal static class StateMountTestKit
{
	public static int UsesRemaining(State state, EAbilityKind kind) =>
		state.UsesRemaining(kind);

	public static int CooldownRemaining(State state, EAbilityKind kind) =>
		state.CooldownRemaining(kind);

	public static void SetUsesRemaining(State state, EAbilityKind kind, int uses) =>
		state.MountRuntimeFor(kind).UsesRemaining = uses;

	public static void SetCooldownRemaining(State state, EAbilityKind kind, int turns) =>
		state.MountRuntimeFor(kind).CooldownRemaining = turns;
}

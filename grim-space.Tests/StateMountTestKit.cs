using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Tests;

internal static class StateMountTestKit
{
	public static int UsesRemaining(State state, EAbilityKind kind) =>
		state.UsesRemaining(kind);

	public static int CooldownRemaining(State state, EAbilityKind kind) =>
		state.Loadout.InstalledAbilities
			.Where(installed => installed.Spec.Kind == kind)
			.Select(installed => state.CooldownRemaining(installed.Mount))
			.DefaultIfEmpty()
			.Max();

	public static int UsesRemaining(
		State state,
		EAbilityKind kind,
		ESpatialOrientation mountedOn) =>
		state.UsesRemaining(new AbilityMount(kind, mountedOn));

	public static int CooldownRemaining(
		State state,
		EAbilityKind kind,
		ESpatialOrientation mountedOn) =>
		state.CooldownRemaining(new AbilityMount(kind, mountedOn));

	public static void SetUsesRemaining(State state, EAbilityKind kind, int uses)
	{
		foreach (var mount in state.MountRuntime.Keys.Where(mount => mount.Kind == kind))
			state.MountRuntimeFor(mount).UsesRemaining = uses;
	}

	public static void SetCooldownRemaining(State state, EAbilityKind kind, int turns)
	{
		foreach (var mount in state.MountRuntime.Keys.Where(mount => mount.Kind == kind))
			state.MountRuntimeFor(mount).CooldownRemaining = turns;
	}
}

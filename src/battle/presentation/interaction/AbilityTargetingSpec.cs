using Godot;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Interaction;

public readonly record struct AbilitySourcePose(
	Coord Position,
	Coord Fore,
	Coord Dorsal,
	ESpatialOrientation? MountedOn = null);

public sealed record AbilityTargetingSpec(
	Func<State, IAction, AbilitySourcePose> ResolveSource,
	Func<Mesh> CreateGhostMesh,
	Color Tint);

using Godot;
using GrimSpace.Components;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Presentation.Ui;

public static class UnitPortraitCatalog
{
	private const string PatrolPortraitPath = "res://assets/ui/unit_portraits/patrol.png";
	private const string FighterPortraitPath = "res://assets/ui/unit_portraits/fighter.png";
	private const string CarrierPortraitPath = "res://assets/ui/unit_portraits/carrier.png";
	private const string VoidBombPortraitPath = "res://assets/ui/unit_portraits/void_bomb.png";
	private const int PortraitSize = 52;

	private static Texture2D? _fighter;
	private static Texture2D? _carrier;
	private static Texture2D? _patrol;
	private static Texture2D? _voidBomb;

	public static Texture2D? For(EType type) =>
		type switch
		{
			EType.Fighter => _fighter ??= SvgIconLoader.LoadRaw(FighterPortraitPath, PortraitSize),
			EType.Carrier => _carrier ??= SvgIconLoader.LoadRaw(CarrierPortraitPath, PortraitSize),
			EType.Patrol => _patrol ??= SvgIconLoader.LoadRaw(PatrolPortraitPath, PortraitSize),
			EType.VoidBomb => _voidBomb ??= SvgIconLoader.LoadRaw(VoidBombPortraitPath, PortraitSize),
			_ => null,
		};
}

using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Weapons;

namespace GrimSpace.Battle.Presentation.Domains.Weapons;

public static class PlayerWeaponLegality
{
	public readonly record struct Legality(bool Flak, bool Railgun, bool Torpedo);

	public static Legality For(BattleOrchestrator battle)
	{
		var sim = battle.Sim;
		var playerId = battle.PlayerId;
		var type = sim.StateOf<ActorState>(playerId).Type;

		return new Legality(
			Flak: Capabilities.For(type)
				.OfType<FlakDef>()
				.Any(def => sim.Peek(new FlakAction(playerId, def.Mount)) is not null),
			Railgun: sim.Peek(new RailgunAction(playerId)) is not null,
			Torpedo: TorpedoConfig.EnabledMounts
				.Any(mount => sim.Peek(new TorpedoAction(playerId, mount)) is not null));
	}
}

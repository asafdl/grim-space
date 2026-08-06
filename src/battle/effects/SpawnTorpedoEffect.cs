using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Weapons;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Effects;

public sealed class SpawnTorpedoEffect(ETorpedoMount mount) : IEffect<BattleWorld, ActorRuntime>
{
	private Unit? _spawned;

	public void Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var firer = world.UnitOf(actorId);
		var (position, fore, dorsal) = TorpedoMount.LaunchPose(firer.State, mount);
		var torpedo = Factory.Create(
			new Instance
			{
				Id = string.Empty,
				Type = EType.Torpedo,
				Controller = firer.Controller,
			},
			position,
			world.IdRegistry,
			TorpedoConfig.SpawnMomentum,
			fore,
			dorsal);
		torpedo.State.FuelRemaining = TorpedoConfig.Fuel;
		world.MutableUnits[torpedo.State.Id] = torpedo;
		_spawned = torpedo;
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		if (_spawned is null)
			return;

		world.MutableUnits.Remove(_spawned.State.Id);
		_spawned = null;
	}
}

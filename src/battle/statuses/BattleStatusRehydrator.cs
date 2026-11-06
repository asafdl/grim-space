using GrimSpace.Battle.Units;
using GrimSpace.Pilots;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Battle.Statuses;

public static class BattleStatusRehydrator
{
	public static BattleStatus Rehydrate(BattleStatus persisted)
	{
		ArgumentNullException.ThrowIfNull(persisted);
		var parts = persisted.InstanceId.Split(':', 3);
		if (parts.Length != 3 || parts[2].Length == 0)
			throw new InvalidDataException($"Malformed status instance id '{persisted.InstanceId}'.");

		var (scope, key, rest) = (parts[0], parts[1], parts[2]);
		BattleStatus rebuilt;
		switch (scope, key)
		{
			case ("passive", _) when Enum.TryParse<EPilotCapabilityId>(key, out var capability)
				&& capability.ToString() == key:
				rebuilt = BattlePassiveStatusInstaller.Create(capability, rest);
				break;
			case ("buff", "Overdrive"):
				rebuilt = BattleStatuses.CreateOverdrive(rest);
				break;
			case ("buff", "Overheat"):
				rebuilt = BattleStatuses.CreateOverheat(rest);
				break;
			case ("buff", nameof(EAbilityKind.ProtectionDrone)):
				rebuilt = BattleStatuses.CreateProtectionDrone(rest);
				break;
			case ("buff", nameof(EPilotCapabilityId.SpawnedAllyDamageBonus)):
				rebuilt = BattleStatuses.CreateSpawnedAllyDamageBuff(rest);
				break;
			case ("buff", "ShieldBrace") when persisted.Payload is ShieldBracePayload:
			case ("buff", "TemporaryShield") when persisted.Payload is TemporaryShieldPayload:
				return persisted;
			case ("buff", "MarkWeakspot") when persisted.Payload is MarkWeakspotPayload mark:
				rebuilt = BattleStatuses.CreateMarkWeakspot(rest, mark.Face);
				break;
			default:
				throw new InvalidDataException($"Unknown battle status '{persisted.InstanceId}'.");
		}

		return rebuilt.InstanceId == persisted.InstanceId
			? rebuilt
			: throw new InvalidDataException($"Unknown battle status '{persisted.InstanceId}'.");
	}
}

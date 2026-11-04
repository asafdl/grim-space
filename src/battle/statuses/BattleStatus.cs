using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Battle.Statuses;

public enum EStatusPolarity
{
	Buff,
	Debuff,
	Neutral,
}

public readonly record struct IncomingHit(int Damage, int ShieldDamageBonus);

public interface IBattleStatusPayload
{
	IBattleStatusPayload DeepCopy();
}

public sealed class EmptyStatusPayload : IBattleStatusPayload
{
	public static EmptyStatusPayload Instance { get; } = new();

	public IBattleStatusPayload DeepCopy() => this;
}

public sealed class BattleStatus
{
	public required string InstanceId { get; init; }
	public required int Priority { get; init; }
	public required IReadOnlyList<string> ActionTags { get; init; }
	public IBattleStatusPayload Payload { get; init; } = EmptyStatusPayload.Instance;
	public EStatusPolarity Polarity { get; init; } = EStatusPolarity.Neutral;
	public Func<IAction, BattleWorld, IEffect<BattleWorld, ActorRuntime>, IEffect<BattleWorld, ActorRuntime>>? RewriteOutgoing { get; init; }
	public Func<IAreaDamage, IReadOnlyList<string>, IAreaDamage>? RewriteArea { get; init; }
	public Func<int, int>? RewriteIncomingDamage { get; init; }
	public Func<BattleWorld, Unit, EImpactCause, ESpatialOrientation, IncomingHit, IncomingHit>? RewriteIncomingHit { get; init; }
	public Func<int, int>? RewriteShieldDamage { get; init; }
	public Func<int, int>? RewriteHullDamage { get; init; }

	public BattleStatus DeepCopy() =>
		new()
		{
			InstanceId = InstanceId,
			Priority = Priority,
			ActionTags = ActionTags,
			Payload = Payload.DeepCopy(),
			Polarity = Polarity,
			RewriteOutgoing = RewriteOutgoing,
			RewriteArea = RewriteArea,
			RewriteIncomingDamage = RewriteIncomingDamage,
			RewriteIncomingHit = RewriteIncomingHit,
			RewriteShieldDamage = RewriteShieldDamage,
			RewriteHullDamage = RewriteHullDamage,
		};

	public bool MatchesAction(IReadOnlyList<string> actionTags) =>
		ActionTags.Count == 0 || ActionTags.All(actionTags.Contains);
}

public static class BattleStatuses
{
	public static IEnumerable<BattleStatus> Ordered(IEnumerable<BattleStatus> statuses) =>
		statuses
			.Select((status, index) => (status, index))
			.OrderBy(entry => entry.status.Priority)
			.ThenBy(entry => entry.index)
			.Select(entry => entry.status);

	public static void CopyInto(IEnumerable<BattleStatus> source, List<BattleStatus> destination)
	{
		destination.Clear();
		foreach (var status in source)
			destination.Add(status.DeepCopy());
	}

	public static void RemoveInstance(List<BattleStatus> statuses, string instanceId) =>
		statuses.RemoveAll(status => status.InstanceId == instanceId);

	public static string PassiveInstanceId(string key, string actorId) =>
		$"passive:{key}:{actorId}";

	public static string BuffInstanceId(string key, string actorId) =>
		$"buff:{key}:{actorId}";

	public static bool HasOverheat(State state)
	{
		ArgumentNullException.ThrowIfNull(state);
		var instanceId = BuffInstanceId("Overheat", state.Id);
		return state.Statuses.Exists(status => status.InstanceId == instanceId);
	}

	public static bool HasOneWithTheVoid(State state)
	{
		ArgumentNullException.ThrowIfNull(state);
		var instanceId = PassiveInstanceId(nameof(EPilotCapabilityId.OneWithTheVoid), state.Id);
		return state.Statuses.Exists(status => status.InstanceId == instanceId);
	}

	public static bool HasDefensiveManeuvers(State state)
	{
		ArgumentNullException.ThrowIfNull(state);
		var instanceId = PassiveInstanceId(
			nameof(EPilotCapabilityId.StationaryDamageDeflection),
			state.Id);
		return state.Statuses.Exists(status => status.InstanceId == instanceId);
	}

	public static bool HasShieldDynamo(State state)
	{
		ArgumentNullException.ThrowIfNull(state);
		var instanceId = PassiveInstanceId(nameof(EPilotCapabilityId.ShieldDynamo), state.Id);
		return state.Statuses.Exists(status => status.InstanceId == instanceId);
	}

	public static bool HasThirstForScrap(State state)
	{
		ArgumentNullException.ThrowIfNull(state);
		var instanceId = PassiveInstanceId(nameof(EPilotCapabilityId.ThirstForScrap), state.Id);
		return state.Statuses.Exists(status => status.InstanceId == instanceId);
	}

	public static void CopyOneWithTheVoidOnto(State source, State destination)
	{
		ArgumentNullException.ThrowIfNull(source);
		ArgumentNullException.ThrowIfNull(destination);
		var destinationId = PassiveInstanceId(nameof(EPilotCapabilityId.OneWithTheVoid), destination.Id);
		RemoveInstance(destination.Statuses, destinationId);
		if (HasOneWithTheVoid(source))
			destination.Statuses.Add(
				BattlePassiveStatusInstaller.Create(EPilotCapabilityId.OneWithTheVoid, destination.Id));
	}

	public static BattleStatus CreateOverdrive(string actorId) =>
		new()
		{
			InstanceId = BuffInstanceId("Overdrive", actorId),
			Priority = 50,
			ActionTags = [BattleActionTags.Attack],
			Polarity = EStatusPolarity.Buff,
			RewriteOutgoing = (_, world, effect) =>
			{
				var bonus = world.StateOf(actorId).Loadout.OverdriveDamageBonusPercent;
				return effect switch
				{
					ApplyAreaDamageEffect area => area.WithAddedDamageBonusPercent(bonus, includesOverdrive: true),
					ApplyChainLightningDamageEffect chain => chain.WithAddedDamageBonusPercent(
						bonus,
						includesOverdrive: true),
					_ => effect,
				};
			},
		};

	public static BattleStatus CreateOverheat(string actorId) =>
		new()
		{
			InstanceId = BuffInstanceId("Overheat", actorId),
			Priority = 50,
			ActionTags = [],
			Polarity = EStatusPolarity.Debuff,
		};

	public static BattleStatus CreateProtectionDrone(string actorId) =>
		new()
		{
			InstanceId = BuffInstanceId(nameof(EAbilityKind.ProtectionDrone), actorId),
			Priority = 210,
			ActionTags = [],
			Polarity = EStatusPolarity.Buff,
			RewriteIncomingHit = (_, _, _, _, hit) =>
				hit.Damage <= 0
					? hit
					: Defense.ScaleHitDamageTaken(
						hit,
						ProtectionDroneSpec.Baseline.DamageTakenPercent),
		};

	public static BattleStatus CreateMarkWeakspot(string targetId, ESpatialOrientation face) =>
		new()
		{
			InstanceId = BuffInstanceId("MarkWeakspot", targetId),
			Priority = 220,
			ActionTags = [],
			Polarity = EStatusPolarity.Debuff,
			Payload = new MarkWeakspotPayload { Face = face },
			RewriteIncomingHit = (_, _, _, hitFace, hit) =>
			{
				if (hit.Damage <= 0 || hitFace != face)
					return hit;
				return hit with
				{
					ShieldDamageBonus = hit.ShieldDamageBonus
						+ Defense.ScalePercent(
							hit.Damage,
							PilotSkillPercents.MarkWeakspot.ShieldDamageBonusPercent),
				};
			},
		};

	public static void SyncOverdrive(State state)
	{
		ArgumentNullException.ThrowIfNull(state);
		RemoveInstance(state.Statuses, BuffInstanceId("Overdrive", state.Id));
		if (state.Overdrive && state.HeatProfile.Participates)
			state.Statuses.Add(CreateOverdrive(state.Id));
	}

	public static void SyncOverheat(State state)
	{
		ArgumentNullException.ThrowIfNull(state);
		RemoveInstance(state.Statuses, BuffInstanceId("Overheat", state.Id));
		if (state.Overheat && state.HeatProfile.Participates)
			state.Statuses.Add(CreateOverheat(state.Id));
	}

	public static IAreaDamage RewriteArea(
		IAreaDamage area,
		IEnumerable<BattleStatus> statuses,
		IReadOnlyList<string> actionTags)
	{
		var current = area;
		foreach (var status in Ordered(statuses))
		{
			if (!status.MatchesAction(actionTags) || status.RewriteArea is null)
				continue;
			current = status.RewriteArea(current, actionTags);
		}

		return current;
	}

	public static IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> RewriteOutgoing(
		IAction action,
		BattleWorld world,
		IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> effects)
	{
		if (effects.Count == 0)
			return effects;

		State actor;
		try
		{
			actor = world.StateOf(action.ActorId);
		}
		catch (KeyNotFoundException)
		{
			return effects;
		}

		IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> rewritten = effects;
		if (actor.Statuses.Count > 0)
		{
			var ordered = Ordered(actor.Statuses)
				.Where(status => status.MatchesAction(action.Tags) && status.RewriteOutgoing is not null)
				.ToArray();
			if (ordered.Length > 0)
			{
				var folded = new List<IEffect<BattleWorld, ActorRuntime>>(effects.Count);
				foreach (var effect in effects)
				{
					var current = effect;
					foreach (var status in ordered)
					{
						current = status.RewriteOutgoing!(action, world, current)
							?? throw new InvalidOperationException(
								$"Status '{status.InstanceId}' returned a null effect rewrite.");
					}

					folded.Add(current);
				}

				rewritten = folded;
			}
		}

		var weaponPercent = actor.Loadout.WeaponDamagePercent;
		if (weaponPercent == 0)
			return rewritten;

		var withWeaponDamage = new List<IEffect<BattleWorld, ActorRuntime>>(rewritten.Count);
		foreach (var effect in rewritten)
			withWeaponDamage.Add(AddWeaponDamagePercent(effect, weaponPercent));
		return withWeaponDamage;
	}

	private static IEffect<BattleWorld, ActorRuntime> AddWeaponDamagePercent(
		IEffect<BattleWorld, ActorRuntime> effect,
		int percent) =>
		effect switch
		{
			ApplyAreaDamageEffect area => area.WithAddedDamageBonusPercent(percent),
			ApplyChainLightningDamageEffect chain => chain.WithAddedDamageBonusPercent(percent),
			_ => effect,
		};

	public static int RewriteIncomingDamage(IEnumerable<BattleStatus> statuses, int damage) =>
		FoldChannel(statuses, damage, status => status.RewriteIncomingDamage);

	public static IncomingHit RewriteIncomingHit(
		IEnumerable<BattleStatus> statuses,
		BattleWorld world,
		Unit target,
		EImpactCause cause,
		ESpatialOrientation face,
		IncomingHit hit)
	{
		var current = hit;
		foreach (var status in Ordered(statuses))
		{
			if (status.RewriteIncomingHit is { } rewrite)
				current = rewrite(world, target, cause, face, current);
		}

		return current;
	}

	public static int RewriteShieldDamage(IEnumerable<BattleStatus> statuses, int shieldDamage) =>
		FoldChannel(statuses, shieldDamage, status => status.RewriteShieldDamage);

	public static int RewriteHullDamage(IEnumerable<BattleStatus> statuses, int hullDamage) =>
		FoldChannel(statuses, hullDamage, status => status.RewriteHullDamage);

	private static int FoldChannel(
		IEnumerable<BattleStatus> statuses,
		int amount,
		Func<BattleStatus, Func<int, int>?> select)
	{
		if (amount <= 0)
			return amount;

		var current = amount;
		foreach (var status in Ordered(statuses))
		{
			if (select(status) is { } rewrite)
				current = rewrite(current);
		}

		return current;
	}
}

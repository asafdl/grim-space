using System.Text;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Presentation;

public static class ActionLog
{
	public static IReadOnlyList<string> Format(
		IReadOnlyList<ITimelineEntry> history,
		Func<string, string> displayName)
	{
		var lines = new List<string>();
		string? lastActorId = null;
		var i = 0;

		void Emit(string? actorId, string line)
		{
			if (actorId is not null
				&& lastActorId is not null
				&& !string.Equals(lastActorId, actorId, StringComparison.Ordinal)
				&& lines.Count > 0)
				lines.Add("");

			lines.Add(line);
			if (actorId is not null)
				lastActorId = actorId;
		}

		while (i < history.Count)
		{
			var entry = history[i];
			if (entry is MoveStepAction move)
			{
				var actorId = move.ActorId;
				var steps = 0;
				while (i < history.Count && IsPathManeuver(history[i], actorId))
				{
					if (history[i] is MoveStepAction)
						steps++;
					i++;
				}

				Emit(actorId, $"{displayName(actorId)} moved {steps} {(steps == 1 ? "step" : "steps")}");
				continue;
			}

			if (TryWeaponVerb(entry, out var actorIdWeapon, out var verb))
			{
				i++;
				var impacts = TakeFollowingImpacts(history, ref i, actorIdWeapon);
				if (impacts.Count == 0)
				{
					Emit(actorIdWeapon, $"{displayName(actorIdWeapon)} {verb} → Miss!");
					continue;
				}

				foreach (var impact in impacts)
					Emit(actorIdWeapon, $"{displayName(actorIdWeapon)} {verb} → {FormatHitClause(impact, displayName)}");
				continue;
			}

			if (entry is SpawnPatrolAction deploy)
			{
				var patrolId = ResolveSpawnedPatrolId(deploy, history, i);
				Emit(deploy.ActorId, $"{displayName(deploy.ActorId)} deployed {displayName(patrolId)}");
				i++;
				continue;
			}

			var line = FormatOne(entry, displayName);
			if (line is not null)
				Emit(ActorIdOf(entry), line);
			i++;
		}

		return lines;
	}

	public static string DisplayName(UnitRegistry units, string id)
	{
		if (!units.TryGet(id, out var unit))
			return id;

		return $"{TeamWord(unit.Alliance.Team)} {id}";
	}

	private static bool IsPathManeuver(ITimelineEntry entry, string actorId) =>
		entry switch
		{
			MoveStepAction a => a.ActorId == actorId,
			HeadingTurnAction a => a.ActorId == actorId,
			RollAction a => a.ActorId == actorId,
			_ => false,
		};

	private static string TeamWord(ETeam team) =>
		team switch
		{
			ETeam.Player => "player",
			ETeam.Enemy => "enemy",
			_ => team.ToString().ToLowerInvariant(),
		};

	private static bool TryWeaponVerb(ITimelineEntry entry, out string actorId, out string verb)
	{
		switch (entry)
		{
			case FlakAction a:
				actorId = a.ActorId;
				verb = $"fired flak from {FormatEnum(a.MountedOn)}";
				return true;
			case RailgunAction a:
				actorId = a.ActorId;
				verb = "shot railgun";
				return true;
			case DetonateAction a:
				actorId = a.ActorId;
				verb = "detonated";
				return true;
			default:
				actorId = "";
				verb = "";
				return false;
		}
	}

	private static List<ImpactFacts> TakeFollowingImpacts(
		IReadOnlyList<ITimelineEntry> history,
		ref int i,
		string sourceId)
	{
		var impacts = new List<ImpactFacts>();
		while (i < history.Count
			&& history[i] is Record<ImpactFacts> { Value: var impact }
			&& impact.SourceId == sourceId)
		{
			impacts.Add(impact);
			i++;
		}

		return impacts;
	}

	private static string? ActorIdOf(ITimelineEntry entry) =>
		entry switch
		{
			IAction action => action.ActorId,
			_ => null,
		};

	private static string ResolveSpawnedPatrolId(
		SpawnPatrolAction deploy,
		IReadOnlyList<ITimelineEntry> history,
		int index)
	{
		if (deploy.SpawnedUnitId is { } id)
			return id;

		if (index + 1 < history.Count
			&& history[index + 1] is Record<SpawnFacts> { Value: var spawn }
			&& spawn.SourceId == deploy.ActorId
			&& spawn.EntityType == EType.Patrol)
			return spawn.TargetId;

		return "patrol";
	}

	private static string? FormatOne(ITimelineEntry entry, Func<string, string> displayName) =>
		entry switch
		{
			TorpedoAction a => $"{displayName(a.ActorId)} launched torpedo from {FormatEnum(a.MountedOn)}",
			HeadingTurnAction a => $"{displayName(a.ActorId)} turned {FormatEnum(a.Turn)}",
			RollAction a => $"{displayName(a.ActorId)} rolled {FormatEnum(a.Direction)}",
			Record<ImpactFacts> { Value: var impact } =>
				$"hit {FormatDamageClause(impact, displayName)}",
			EndOfPhaseAction => null,
			RoundUpkeepAction => null,
			ClearTurnHazardsAction => null,
			FuelBurnAction => null,
			Record<SpawnFacts> => null,
			_ => null,
		};

	private static string FormatHitClause(ImpactFacts impact, Func<string, string> displayName) =>
		$"Hit {FormatDamageClause(impact, displayName)}";

	private static string FormatDamageClause(ImpactFacts impact, Func<string, string> displayName)
	{
		var sb = new StringBuilder();
		sb.Append(displayName(impact.TargetId));
		sb.Append(" at ");
		sb.Append(FormatEnum(impact.Face));

		var parts = new List<string>(3);
		if (impact.ShieldDamage > 0)
			parts.Add($"{impact.ShieldDamage} shield damage");
		if (impact.HullDamage > 0)
			parts.Add($"{impact.HullDamage} hull damage");
		if (impact.MomentumLoss > 0)
			parts.Add($"{impact.MomentumLoss} momentum loss");

		if (parts.Count > 0)
		{
			sb.Append(" for ");
			sb.Append(JoinAnd(parts));
		}

		return sb.ToString();
	}

	private static string JoinAnd(IReadOnlyList<string> parts) =>
		parts.Count switch
		{
			1 => parts[0],
			2 => $"{parts[0]} and {parts[1]}",
			_ => string.Join(", ", parts.Take(parts.Count - 1)) + $", and {parts[^1]}",
		};

	private static string FormatEnum<T>(T value) where T : struct, Enum =>
		value.ToString().ToLowerInvariant();
}

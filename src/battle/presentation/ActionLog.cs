using System.Text;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Presentation;

public static class ActionLog
{
	public sealed record Entry(
		string Title,
		IReadOnlyList<string> Metadata,
		bool IsTurnHeader = false);

	public static Entry TurnHeader(int turnNumber) =>
		new($"TURN {turnNumber}", [], IsTurnHeader: true);

	public static IReadOnlyList<Entry> Format(
		IReadOnlyList<ITimelineEntry> history,
		Func<string, string> displayName)
	{
		var entries = new List<Entry>();
		var i = 0;

		void Emit(string title, params string[] metadata) =>
			entries.Add(new Entry(title, metadata));

		while (i < history.Count)
		{
			var entry = history[i];
			if (entry is MoveStepAction or HeadingTurnAction or RollAction)
			{
				var actorId = ((IAction)entry).ActorId;
				var steps = 0;
				while (i < history.Count
					&& history[i] is IAction next
					&& next.ActorId == actorId
					&& next is MoveStepAction or HeadingTurnAction or RollAction)
				{
					if (next is MoveStepAction)
						steps++;
					i++;
				}

				if (steps > 0)
					Emit($"Move · {steps} {(steps == 1 ? "step" : "steps")}", displayName(actorId));
				continue;
			}

			if (entry is TorpedoMoveStepAction)
			{
				var actorId = ((IAction)entry).ActorId;
				var steps = 0;
				while (i < history.Count
					&& history[i] is TorpedoMoveStepAction next
					&& next.ActorId == actorId)
				{
					steps++;
					i++;
				}

				Emit($"Move · {steps} {(steps == 1 ? "step" : "steps")}", displayName(actorId));
				continue;
			}

			if (TryWeapon(entry, out var actorIdWeapon, out var weapon, out var mount))
			{
				i++;
				var impacts = TakeFollowingImpacts(history, ref i, actorIdWeapon);
				if (impacts.Count == 0)
				{
					Emit(
						$"{weapon} · Miss",
						[.. ActorMetadata(displayName(actorIdWeapon), mount)]);
					continue;
				}

				foreach (var impact in impacts)
				{
					Emit(
						$"{weapon} · Hit",
						[.. ActorMetadata(
							$"{displayName(actorIdWeapon)} → {displayName(impact.TargetId)}",
							mount),
							FormatImpactDetail(impact)]);
				}
				continue;
			}

			if (entry is SpawnPatrolAction deploy)
			{
				var patrolId = ResolveSpawnedPatrolId(deploy, history, i);
				Emit(
					"Deploy patrol",
					$"{displayName(deploy.ActorId)} → {displayName(patrolId)}");
				i++;
				continue;
			}

			var formatted = FormatOne(entry, displayName);
			if (formatted is not null)
				entries.Add(formatted);
			i++;
		}

		return entries;
	}

	public static string DisplayName(UnitRegistry units, string id)
	{
		if (!units.TryGet(id, out var unit))
			return id;

		return $"{TeamWord(unit.Alliance.Team)} {id}";
	}

	private static string TeamWord(ETeam team) =>
		team switch
		{
			ETeam.Player => "player",
			ETeam.Enemy => "enemy",
			_ => team.ToString().ToLowerInvariant(),
		};

	private static bool TryWeapon(
		ITimelineEntry entry,
		out string actorId,
		out string weapon,
		out string? mount)
	{
		switch (entry)
		{
			case FlakAction a:
				actorId = a.ActorId;
				weapon = "Flak";
				mount = $"{FormatEnum(a.MountedOn)} mount";
				return true;
			case RailgunAction a:
				actorId = a.ActorId;
				weapon = "Railgun";
				mount = null;
				return true;
			case DetonateAction a:
				actorId = a.ActorId;
				weapon = "Detonate";
				mount = null;
				return true;
			default:
				actorId = "";
				weapon = "";
				mount = null;
				return false;
		}
	}

	private static IEnumerable<string> ActorMetadata(string actor, string? mount)
	{
		yield return actor;
		if (mount is not null)
			yield return mount;
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

	private static Entry? FormatOne(ITimelineEntry entry, Func<string, string> displayName) =>
		entry switch
		{
			TorpedoAction a => new Entry(
				"Launch torpedo",
				[displayName(a.ActorId), $"{FormatEnum(a.MountedOn)} mount"]),
			HeadingTurnAction a => new Entry(
				$"Turn · {FormatEnum(a.Turn)}",
				[displayName(a.ActorId)]),
			RollAction a => new Entry(
				$"Roll · {FormatEnum(a.Direction)}",
				[displayName(a.ActorId)]),
			Record<ImpactFacts> { Value: var impact } =>
				new Entry(
					$"Impact · {FormatEnum(impact.Cause)}",
					[
						$"{displayName(impact.SourceId)} → {displayName(impact.TargetId)}",
						FormatImpactDetail(impact),
					]),
			EndOfPhaseAction => null,
			RoundUpkeepAction => null,
			FuelBurnAction => null,
			Record<SpawnFacts> => null,
			_ => null,
		};

	private static string FormatImpactDetail(ImpactFacts impact)
	{
		var parts = new List<string>(4) { FormatEnum(impact.Face) };
		if (impact.ShieldDamage > 0)
			parts.Add($"{impact.ShieldDamage} shield");
		if (impact.HullDamage > 0)
			parts.Add($"{impact.HullDamage} hull");
		return string.Join(" · ", parts);
	}

	private static string FormatEnum<T>(T value) where T : struct, Enum
	{
		var text = value.ToString();
		var result = new StringBuilder(text.Length + 4);
		for (var i = 0; i < text.Length; i++)
		{
			var current = text[i];
			if (i > 0 && char.IsUpper(current) && char.IsLower(text[i - 1]))
				result.Append(' ');
			result.Append(char.ToLowerInvariant(current));
		}

		return result.ToString();
	}
}

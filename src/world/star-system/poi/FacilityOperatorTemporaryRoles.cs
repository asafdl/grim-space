using GrimSpace.World.StarSystem.Merchants;

namespace GrimSpace.World.StarSystem.Poi;

public enum EOperatorAssignmentPriority
{
	Merchant = 100,
	Presentation = 200,
	Critical = 300,
}

public sealed class FacilityOperatorTemporaryRoles
{
	private readonly Dictionary<(string FacilityId, string OperatorName), List<Entry>> _assignments = new();
	private readonly Dictionary<(string FacilityId, string OperatorName), int> _contractPlacementPauses = new();

	public void Grant(
		string facilityId,
		string operatorName,
		EFacilityOperatorRole role,
		string sourceId,
		int? acceptsSourcesUntilTick = null) =>
		Grant(
			facilityId,
			operatorName,
			new OperatorInteraction(role),
			PriorityFor(role),
			sourceId,
			acceptsSourcesUntilTick);

	public void Grant(
		string facilityId,
		string operatorName,
		OperatorInteraction interaction,
		EOperatorAssignmentPriority priority,
		string sourceId,
		int? acceptsSourcesUntilTick = null)
	{
		ArgumentException.ThrowIfNullOrEmpty(facilityId);
		ArgumentException.ThrowIfNullOrEmpty(operatorName);
		ArgumentNullException.ThrowIfNull(interaction);
		ArgumentException.ThrowIfNullOrEmpty(sourceId);
		if (!Enum.IsDefined(priority))
			throw new ArgumentOutOfRangeException(nameof(priority), priority, null);
		if (acceptsSourcesUntilTick is { } untilTick)
			ArgumentOutOfRangeException.ThrowIfNegative(untilTick);

		var key = (facilityId, operatorName);
		if (!_assignments.TryGetValue(key, out var entries))
		{
			entries = [];
			_assignments.Add(key, entries);
		}

		var existingSource = entries.FirstOrDefault(entry => entry.SourceIds.Contains(sourceId));
		if (existingSource is not null
			&& (existingSource.Priority != priority
				|| existingSource.Interaction != interaction))
		{
			throw new InvalidOperationException(
				$"Source '{sourceId}' already assigns operator '{operatorName}' at facility " +
				$"'{facilityId}' to '{existingSource.Interaction.Role}'.");
		}

		var samePriority = entries.SingleOrDefault(entry => entry.Priority == priority);
		if (samePriority is not null)
		{
			if (samePriority.Interaction != interaction)
			{
				throw new InvalidOperationException(
					$"Operator '{operatorName}' at facility '{facilityId}' already has interaction " +
					$"'{samePriority.Interaction.Role}' at priority '{priority}' from " +
					$"'{string.Join(", ", samePriority.SourceIds.Order())}'.");
			}

			if (acceptsSourcesUntilTick is not null
				&& samePriority.AcceptsSourcesUntilTick != acceptsSourcesUntilTick)
			{
				throw new InvalidOperationException(
					$"Operator '{operatorName}' at facility '{facilityId}' already has source " +
					$"acceptance deadline '{samePriority.AcceptsSourcesUntilTick?.ToString() ?? "none"}'.");
			}

			samePriority.SourceIds.Add(sourceId);
			return;
		}

		entries.Add(new Entry(
			interaction,
			priority,
			[sourceId],
			acceptsSourcesUntilTick));
	}

	public void GrantPrimary(
		string facilityId,
		string operatorName,
		EFacilityOperatorRole role,
		string sourceId,
		int? acceptsSourcesUntilTick = null) =>
		Grant(
			facilityId,
			operatorName,
			new OperatorInteraction(role),
			EOperatorAssignmentPriority.Critical,
			sourceId,
			acceptsSourcesUntilTick);

	public void RevokeBySource(string sourceId)
	{
		ArgumentException.ThrowIfNullOrEmpty(sourceId);

		foreach (var key in _assignments.Keys.ToArray())
			RemoveSource(_assignments, key, sourceId);
	}

	public void RevokeSource(
		string facilityId,
		string operatorName,
		string sourceId)
	{
		ArgumentException.ThrowIfNullOrEmpty(facilityId);
		ArgumentException.ThrowIfNullOrEmpty(operatorName);
		ArgumentException.ThrowIfNullOrEmpty(sourceId);
		RemoveSource(_assignments, (facilityId, operatorName), sourceId);
	}

	public IReadOnlyList<Assignment> Assignments(EFacilityOperatorRole role) =>
		_assignments
			.Select(pair => (pair.Key, Entry: Active(pair.Value)))
			.Where(item => item.Entry.Interaction.Role == role)
			.OrderBy(item => item.Key.FacilityId, StringComparer.Ordinal)
			.ThenBy(item => item.Key.OperatorName, StringComparer.Ordinal)
			.Select(item => ToAssignment(item.Key, item.Entry))
			.ToArray();

	internal IReadOnlyList<Assignment> AssignmentsIncludingSuspended(
		EFacilityOperatorRole role) =>
		_assignments
			.SelectMany(pair => pair.Value
				.Where(entry => entry.Interaction.Role == role)
				.Select(entry => ToAssignment(pair.Key, entry)))
			.OrderBy(assignment => assignment.FacilityId, StringComparer.Ordinal)
			.ThenBy(assignment => assignment.OperatorName, StringComparer.Ordinal)
			.ThenByDescending(assignment => assignment.Priority)
			.ToArray();

	public bool TryGetAssignment(
		string facilityId,
		string operatorName,
		out Assignment assignment)
	{
		if (_assignments.TryGetValue((facilityId, operatorName), out var entries))
		{
			assignment = ToAssignment((facilityId, operatorName), Active(entries));
			return true;
		}

		assignment = null!;
		return false;
	}

	public bool TryGetInteraction(
		string facilityId,
		string operatorName,
		out OperatorInteraction interaction)
	{
		if (TryGetAssignment(facilityId, operatorName, out var assignment))
		{
			interaction = assignment.Interaction;
			return true;
		}

		interaction = null!;
		return false;
	}

	public bool TryGetRole(string facilityId, string operatorName, out EFacilityOperatorRole role)
	{
		if (TryGetInteraction(facilityId, operatorName, out var interaction))
		{
			role = interaction.Role;
			return true;
		}

		role = default;
		return false;
	}

	public bool HasAssignment(string facilityId, string operatorName) =>
		_assignments.ContainsKey((facilityId, operatorName));

	internal bool HasAssignment(
		string facilityId,
		string operatorName,
		EFacilityOperatorRole role) =>
		_assignments.TryGetValue((facilityId, operatorName), out var entries)
		&& entries.Any(entry => entry.Interaction.Role == role);

	public bool IsAcceptingSources(
		string facilityId,
		string operatorName,
		EFacilityOperatorRole role,
		int currentTick) =>
		TryGetAssignment(facilityId, operatorName, out var assignment)
		&& assignment.Role == role
		&& (assignment.AcceptsSourcesUntilTick is null
			|| currentTick < assignment.AcceptsSourcesUntilTick);

	public void RenewSourceAcceptance(
		string facilityId,
		string operatorName,
		int acceptsSourcesUntilTick)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(acceptsSourcesUntilTick);

		if (!_assignments.TryGetValue((facilityId, operatorName), out var entries))
			throw new InvalidOperationException(
				$"Operator '{operatorName}' at facility '{facilityId}' has no temporary interaction.");

		Active(entries).AcceptsSourcesUntilTick = acceptsSourcesUntilTick;
	}

	public void PauseContractPlacement(
		string facilityId,
		string operatorName,
		int untilTick)
	{
		ArgumentException.ThrowIfNullOrEmpty(facilityId);
		ArgumentException.ThrowIfNullOrEmpty(operatorName);
		ArgumentOutOfRangeException.ThrowIfNegative(untilTick);

		var key = (facilityId, operatorName);
		if (!_contractPlacementPauses.TryGetValue(key, out var currentUntilTick)
			|| untilTick > currentUntilTick)
			_contractPlacementPauses[key] = untilTick;
	}

	public bool IsContractPlacementPaused(
		string facilityId,
		string operatorName,
		int currentTick) =>
		_contractPlacementPauses.TryGetValue((facilityId, operatorName), out var untilTick)
		&& currentTick < untilTick;

	public void PruneExpiredContractPlacementPauses(int currentTick)
	{
		foreach (var key in _contractPlacementPauses
			.Where(pair => pair.Value <= currentTick)
			.Select(pair => pair.Key)
			.ToArray())
		{
			_contractPlacementPauses.Remove(key);
		}
	}

	public void PruneSources(EFacilityOperatorRole role, IReadOnlySet<string> retainedSourceIds)
	{
		ArgumentNullException.ThrowIfNull(retainedSourceIds);

		foreach (var key in _assignments.Keys.ToArray())
		{
			var entries = _assignments[key];
			foreach (var entry in entries.Where(entry => entry.Interaction.Role == role))
				entry.SourceIds.RemoveWhere(sourceId => !retainedSourceIds.Contains(sourceId));
			entries.RemoveAll(entry => entry.SourceIds.Count == 0);
			if (entries.Count == 0)
				_assignments.Remove(key);
		}
	}

	public void Clear(EFacilityOperatorRole role)
	{
		foreach (var key in _assignments.Keys.ToArray())
		{
			var entries = _assignments[key];
			entries.RemoveAll(entry => entry.Interaction.Role == role);
			if (entries.Count == 0)
				_assignments.Remove(key);
		}
	}

	public FacilityOperatorTemporaryRoles CloneForFork()
	{
		var clone = new FacilityOperatorTemporaryRoles();
		foreach (var (key, entries) in _assignments)
			clone._assignments[key] = entries.Select(Clone).ToList();
		foreach (var (key, untilTick) in _contractPlacementPauses)
			clone._contractPlacementPauses[key] = untilTick;
		return clone;
	}

	internal IReadOnlyList<SnapshotEntry> Snapshot() =>
		_assignments
			.SelectMany(pair => pair.Value
				.Where(entry => entry.Interaction.Role is not (
					EFacilityOperatorRole.Contracts
						or EFacilityOperatorRole.LeadContact))
				.SelectMany(entry => entry.SourceIds.Select(sourceId => new SnapshotEntry(
					pair.Key.FacilityId,
					pair.Key.OperatorName,
					entry.Interaction.Role,
					entry.Interaction.MerchantSubtype,
					sourceId,
					entry.Priority,
					entry.AcceptsSourcesUntilTick))))
			.ToArray();

	internal static FacilityOperatorTemporaryRoles FromSnapshot(
		IEnumerable<SnapshotEntry> entries)
	{
		var assignments = new FacilityOperatorTemporaryRoles();
		foreach (var entry in entries)
		{
			if (entry.Role is EFacilityOperatorRole.Contracts
				or EFacilityOperatorRole.LeadContact)
				continue;
			assignments.Grant(
				entry.FacilityId,
				entry.OperatorName,
				new OperatorInteraction(entry.Role, entry.MerchantSubtype),
				entry.Priority,
				entry.SourceId,
				entry.AcceptsSourcesUntilTick);
		}
		return assignments;
	}

	internal static FacilityOperatorTemporaryRoles FromSnapshot(
		IEnumerable<(string FacilityId, string OperatorName, EFacilityOperatorRole Role, string SourceId)> entries) =>
		FromSnapshot(entries.Select(entry => new SnapshotEntry(
			entry.FacilityId,
			entry.OperatorName,
			entry.Role,
			null,
			entry.SourceId,
			PriorityFor(entry.Role),
			null)));

	internal static EOperatorAssignmentPriority PriorityFor(EFacilityOperatorRole role) =>
		role switch
		{
			EFacilityOperatorRole.Merchant => EOperatorAssignmentPriority.Merchant,
			EFacilityOperatorRole.Contracts
				or EFacilityOperatorRole.StoryContact
				or EFacilityOperatorRole.LeadContact =>
				EOperatorAssignmentPriority.Presentation,
			EFacilityOperatorRole.DeliveryTurnIn => EOperatorAssignmentPriority.Critical,
			_ => throw new ArgumentException(
				$"Role '{role}' cannot be assigned as a temporary interaction.",
				nameof(role)),
		};

	private static void RemoveSource(
		Dictionary<(string FacilityId, string OperatorName), List<Entry>> assignments,
		(string FacilityId, string OperatorName) key,
		string sourceId)
	{
		if (!assignments.TryGetValue(key, out var entries))
			return;

		foreach (var entry in entries)
			entry.SourceIds.Remove(sourceId);
		entries.RemoveAll(entry => entry.SourceIds.Count == 0);
		if (entries.Count == 0)
			assignments.Remove(key);
	}

	private static Entry Active(IReadOnlyList<Entry> entries) =>
		entries.MaxBy(entry => entry.Priority)
		?? throw new InvalidOperationException("Operator assignment set cannot be empty.");

	private static Entry Clone(Entry entry) =>
		new(
			entry.Interaction,
			entry.Priority,
			new HashSet<string>(entry.SourceIds, StringComparer.Ordinal),
			entry.AcceptsSourcesUntilTick);

	private static Assignment ToAssignment(
		(string FacilityId, string OperatorName) key,
		Entry entry) =>
		new(
			key.FacilityId,
			key.OperatorName,
			entry.Interaction,
			entry.Priority,
			entry.SourceIds.Order(StringComparer.Ordinal).ToArray(),
			entry.AcceptsSourcesUntilTick);

	public sealed record Assignment(
		string FacilityId,
		string OperatorName,
		OperatorInteraction Interaction,
		EOperatorAssignmentPriority Priority,
		IReadOnlyList<string> SourceIds,
		int? AcceptsSourcesUntilTick)
	{
		public EFacilityOperatorRole Role => Interaction.Role;
		public EMerchantSubtype? MerchantSubtype => Interaction.MerchantSubtype;
	}

	internal sealed record SnapshotEntry(
		string FacilityId,
		string OperatorName,
		EFacilityOperatorRole Role,
		EMerchantSubtype? MerchantSubtype,
		string SourceId,
		EOperatorAssignmentPriority Priority,
		int? AcceptsSourcesUntilTick);

	private sealed class Entry(
		OperatorInteraction interaction,
		EOperatorAssignmentPriority priority,
		HashSet<string> sourceIds,
		int? acceptsSourcesUntilTick)
	{
		public OperatorInteraction Interaction { get; } = interaction;
		public EOperatorAssignmentPriority Priority { get; } = priority;
		public HashSet<string> SourceIds { get; } = sourceIds;
		public int? AcceptsSourcesUntilTick { get; set; } = acceptsSourcesUntilTick;
	}
}

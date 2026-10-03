using System.Collections.ObjectModel;
using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Objectives;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Contact;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Resources;
using BattleShipType = GrimSpace.Units.Enums.EType;

namespace GrimSpace.Run.Persistence;

public sealed class PersistenceRegistry
{
	private static readonly JsonSerializerOptions JsonOptions =
		ReflectionJson.CreateOptions(options =>
		{
			options.IncludeFields = true;
			options.Converters.Add(new ContractSpawnMemberJsonConverter());
			options.Converters.Add(new ResourceBundleJsonConverter());
			options.Converters.Add(new TransitPathJsonConverter());
			options.Converters.Add(new ContactTargetJsonConverter());
			options.Converters.Add(new WreckageOutcomeJsonConverter());
		});
	private readonly Dictionary<string, Func<JsonElement, object>> _readers =
		new(StringComparer.Ordinal);
	private readonly Dictionary<Type, (string Discriminator, Func<object, JsonElement> Writer)> _writers = [];

	public IReadOnlyCollection<string> Discriminators =>
		new ReadOnlyCollection<string>(_readers.Keys.ToArray());

	internal JsonSerializerOptions Options => JsonOptions;

	public static PersistenceRegistry CreateDefault()
	{
		var registry = new PersistenceRegistry();

		registry.Register(
			DiscriminatorFor(typeof(AcceptContractAction)),
			ReadAcceptContract,
			WriteAcceptContract);
		registry.Register(
			DiscriminatorFor(typeof(DeclineContractAction)),
			ReadDeclineContract,
			WriteDeclineContract);
		registry.Register(
			DiscriminatorFor(typeof(TurnInDeliveryAction)),
			ReadTurnInDelivery,
			WriteTurnInDelivery);
		registry.Register(
			DiscriminatorFor(typeof(ResolveEngagementAction)),
			ReadResolveEngagement,
			WriteResolveEngagement);
		registry.Register(
			DiscriminatorFor(typeof(PurchaseAction)),
			ReadPurchase,
			WritePurchase);
		registry.Register(
			DiscriminatorFor(typeof(MaintainContractBoardAction)),
			element => ReadMaintainContractBoard(element, registry),
			value => WriteMaintainContractBoard(value, registry));
		registry.Register(
			DiscriminatorFor(typeof(InvestigateWreckageAction)),
			ReadInvestigateWreckage,
			WriteInvestigateWreckage);

		registry.RegisterDiscoveredActions(typeof(PersistenceRegistry).Assembly);

		registry.Register("record.battle.spawn", ReadSpawnFacts, WriteSpawnFacts);

		return registry;
	}

	public void Register<T>(
		string discriminator,
		Func<JsonElement, T> reader,
		Func<T, JsonElement> writer)
	{
		ArgumentException.ThrowIfNullOrEmpty(discriminator);
		ArgumentNullException.ThrowIfNull(reader);
		ArgumentNullException.ThrowIfNull(writer);
		if (!_readers.TryAdd(discriminator, value => reader(value)!))
			throw new InvalidOperationException(
				$"Persistence discriminator '{discriminator}' is already registered.");
		if (!_writers.TryAdd(typeof(T), (discriminator, value => writer((T)value))))
			throw new InvalidOperationException(
				$"Persistence type '{typeof(T).Name}' is already registered.");
	}

	public JsonElement Write(object value)
	{
		ArgumentNullException.ThrowIfNull(value);
		RegisterConventionType(value.GetType());
		if (!_writers.TryGetValue(value.GetType(), out var registration))
			throw new InvalidOperationException(
				$"No persistence registration exists for '{value.GetType().FullName}'.");

		return JsonSerializer.SerializeToElement(
			new DiscriminatedPayload(registration.Discriminator, registration.Writer(value)));
	}

	public object Read(JsonElement value)
	{
		if (value.ValueKind != JsonValueKind.Object
			|| !value.TryGetProperty("type", out var typeProperty)
			|| typeProperty.ValueKind != JsonValueKind.String
			|| !value.TryGetProperty("payload", out var payload))
			throw new InvalidDataException("Discriminated persistence payload is malformed.");

		var discriminator = typeProperty.GetString();
		if (discriminator is not null)
			RegisterConventionDiscriminator(discriminator);
		if (discriminator is null || !_readers.TryGetValue(discriminator, out var reader))
			throw new InvalidDataException(
				$"Unknown persistence discriminator '{discriminator ?? "<missing>"}'.");

		return reader(payload);
	}

	private void RegisterDiscoveredActions(Assembly assembly)
	{
		foreach (var type in assembly.GetTypes()
			.Where(type =>
				type.IsClass
				&& !type.IsAbstract
				&& !type.ContainsGenericParameters
				&& typeof(IAction).IsAssignableFrom(type))
			.OrderBy(type => type.FullName, StringComparer.Ordinal))
		{
			if (_writers.ContainsKey(type))
				continue;

			var discriminator = DiscriminatorFor(type);
			if (_readers.ContainsKey(discriminator))
				throw new InvalidOperationException(
					$"Persistence discriminator '{discriminator}' is used by multiple types.");

			_readers.Add(
				discriminator,
				element => ReflectionJson.Read(element, type, JsonOptions));
			_writers.Add(
				type,
				(discriminator, value => ReflectionJson.Write(value, JsonOptions)));
		}
	}

	private void RegisterConventionType(Type type)
	{
		if (_writers.ContainsKey(type)
			|| !type.IsGenericType
			|| type.GetGenericTypeDefinition() != typeof(Record<>))
			return;

		var discriminator = DiscriminatorForRecord(type.GetGenericArguments()[0]);
		if (_readers.ContainsKey(discriminator))
			return;
		_readers.Add(
			discriminator,
			element => ReflectionJson.Read(element, type, JsonOptions));
		_writers.Add(
			type,
			(discriminator, value => ReflectionJson.Write(value, JsonOptions)));
	}

	private void RegisterConventionDiscriminator(string discriminator)
	{
		if (_readers.ContainsKey(discriminator)
			|| !discriminator.StartsWith("record.", StringComparison.Ordinal))
			return;

		var factTypeName = discriminator["record.".Length..];
		var factType = typeof(PersistenceRegistry).Assembly.GetType(factTypeName);
		if (factType is null)
			return;
		RegisterConventionType(typeof(Record<>).MakeGenericType(factType));
	}

	private static string DiscriminatorForRecord(Type factType) =>
		$"record.{factType.FullName}";

	private static string DiscriminatorFor(Type type)
	{
		var category = type.Namespace switch
		{
			var value when value?.Contains(".Battle.", StringComparison.Ordinal) == true =>
				"battle",
			var value when value?.Contains(".StarSystem.", StringComparison.Ordinal) == true =>
				"star",
			_ => "timeline",
		};
		var name = type.Name.EndsWith("Action", StringComparison.Ordinal)
			? type.Name[..^"Action".Length]
			: type.Name;
		return $"{category}.{ToSnakeCase(name)}";
	}

	private static string ToSnakeCase(string value)
	{
		var result = new System.Text.StringBuilder(value.Length + 8);
		for (var index = 0; index < value.Length; index++)
		{
			if (index > 0 && char.IsUpper(value[index]))
				result.Append('_');
			result.Append(char.ToLowerInvariant(value[index]));
		}
		return result.ToString();
	}

	private static PurchaseAction ReadPurchase(JsonElement element)
	{
		var dto = Deserialize<PurchaseDto>(element);
		return new PurchaseAction(
			dto.ActorId,
			dto.PoiId,
			dto.FacilityId,
			dto.OperatorName,
			dto.Catalog,
			dto.Offering,
			SaveDtoMapper.RestoreShip(dto.Before));
	}

	private static JsonElement WritePurchase(PurchaseAction value) =>
		Serialize(new PurchaseDto(
			value.ActorId,
			value.PoiId,
			value.FacilityId,
			value.OperatorName,
			value.Catalog,
			value.Offering,
			SaveDtoMapper.CaptureShip(value.Before)));

	private static MaintainContractBoardAction ReadMaintainContractBoard(
		JsonElement element,
		PersistenceRegistry registry)
	{
		var dto = Deserialize<MaintainContractBoardDto>(element);
		var additions = dto.Additions.Select(contractDto =>
		{
			var restored = SaveDtoMapper.RestoreContract(contractDto, registry);
			if (restored.ExpiresAtTick is not int expiresAtTick)
				throw new InvalidDataException(
					$"Contract '{restored.Contract.Id}' has no expiration tick.");
			return new ContractAddition(restored.Contract, expiresAtTick);
		}).ToArray();
		return new MaintainContractBoardAction(dto.ActorId, dto.Tick, additions);
	}

	private static JsonElement WriteMaintainContractBoard(
		MaintainContractBoardAction value,
		PersistenceRegistry registry) =>
		Serialize(new MaintainContractBoardDto(
			value.ActorId,
			value.Tick,
			value.Additions
				.Select(addition => SaveDtoMapper.CaptureContract(
					(addition.Contract, null, addition.ExpiresAtTick),
					registry))
				.ToArray()));

	private static AcceptContractAction ReadAcceptContract(JsonElement element)
	{
		var dto = Deserialize<AcceptContractDto>(element);
		return new AcceptContractAction(
			dto.ActorId, dto.PoiId, dto.FacilityId, dto.OperatorName,
			dto.ContractId, dto.SpawnIdentity);
	}

	private static JsonElement WriteAcceptContract(AcceptContractAction value) =>
		Serialize(new AcceptContractDto(
			value.ActorId, value.PoiId, value.FacilityId, value.OperatorName,
			value.ContractId, value.SpawnIdentity));

	private static InvestigateWreckageAction ReadInvestigateWreckage(JsonElement element)
	{
		var dto = Deserialize<InvestigateWreckageDto>(element);
		return new InvestigateWreckageAction(
			dto.ActorId, dto.ContractId, dto.AmbushSpawnIdentity);
	}

	private static JsonElement WriteInvestigateWreckage(InvestigateWreckageAction value) =>
		Serialize(new InvestigateWreckageDto(
			value.ActorId, value.ContractId, value.AmbushSpawnIdentity));

	private static DeclineContractAction ReadDeclineContract(JsonElement element)
	{
		var dto = Deserialize<DeclineContractDto>(element);
		return new DeclineContractAction(
			dto.ActorId, dto.PoiId, dto.FacilityId, dto.OperatorName, dto.ContractId);
	}

	private static JsonElement WriteDeclineContract(DeclineContractAction value) =>
		Serialize(new DeclineContractDto(
			value.ActorId, value.PoiId, value.FacilityId, value.OperatorName, value.ContractId));

	private static TurnInDeliveryAction ReadTurnInDelivery(JsonElement element)
	{
		var dto = Deserialize<TurnInDeliveryDto>(element);
		return new TurnInDeliveryAction(
			dto.ActorId, dto.PoiId, dto.FacilityId, dto.OperatorName, dto.ContractId);
	}

	private static JsonElement WriteTurnInDelivery(TurnInDeliveryAction value) =>
		Serialize(new TurnInDeliveryDto(
			value.ActorId, value.PoiId, value.FacilityId, value.OperatorName, value.ContractId));

	private static ResolveEngagementAction ReadResolveEngagement(JsonElement element)
	{
		var dto = Deserialize<ResolveEngagementDto>(element);
		return new ResolveEngagementAction(
			dto.InitiatorId, dto.Outcome, dto.LootRolls, dto.LootTotal);
	}

	private static JsonElement WriteResolveEngagement(ResolveEngagementAction value) =>
		Serialize(new ResolveEngagementDto(
			value.InitiatorId, value.Outcome, value.LootRolls, value.LootTotal));

	private static Record<SpawnFacts> ReadSpawnFacts(JsonElement element)
	{
		var dto = Deserialize<SpawnFactsDto>(element);
		return new Record<SpawnFacts>(new SpawnFacts(
			dto.SourceId,
			dto.TargetId,
			dto.EntityType,
			SaveDtoMapper.RestoreBattleUnitState(dto.SpawnedState)));
	}

	private static JsonElement WriteSpawnFacts(Record<SpawnFacts> value) =>
		Serialize(new SpawnFactsDto(
			value.Value.SourceId,
			value.Value.TargetId,
			value.Value.EntityType,
			SaveDtoMapper.CaptureBattleUnitState(value.Value.SpawnedState)));

	private static JsonElement Serialize<T>(T value) =>
		ReflectionJson.Write(value!, JsonOptions);

	private static T Deserialize<T>(JsonElement value) =>
		ReflectionJson.Read<T>(value, JsonOptions);

	private sealed class ResourceBundleJsonConverter : JsonConverter<ResourceBundle>
	{
		public override ResourceBundle Read(
			ref Utf8JsonReader reader,
			Type typeToConvert,
			JsonSerializerOptions options)
		{
			var values = JsonSerializer.Deserialize<Dictionary<ResourceId, int>>(
				ref reader,
				options) ?? [];
			return ResourceBundle.Create(values);
		}

		public override void Write(
			Utf8JsonWriter writer,
			ResourceBundle value,
			JsonSerializerOptions options) =>
			JsonSerializer.Serialize(
				writer,
				value.ToDictionary(pair => pair.Key, pair => pair.Value),
				options);
	}

	private sealed class TransitPathJsonConverter : JsonConverter<TransitPath>
	{
		public override TransitPath Read(
			ref Utf8JsonReader reader,
			Type typeToConvert,
			JsonSerializerOptions options)
		{
			var dto = JsonSerializer.Deserialize<TransitPathDto>(ref reader, options)
				?? throw new InvalidDataException("Transit path payload is missing.");
			return new TransitPath(dto.Legs
				.Select(leg => new TransitLeg(
					leg.Points.ToImmutableArray(),
					leg.SpeedMultiplier,
					leg.Length))
				.ToImmutableArray());
		}

		public override void Write(
			Utf8JsonWriter writer,
			TransitPath value,
			JsonSerializerOptions options) =>
			JsonSerializer.Serialize(
				writer,
				new TransitPathDto(value.Legs.Select(leg =>
					new TransitLegDto(leg.Points, leg.SpeedMultiplier, leg.Length)).ToArray()),
				options);
	}

	private sealed class ContactTargetJsonConverter : JsonConverter<ContactTarget>
	{
		public override ContactTarget Read(
			ref Utf8JsonReader reader,
			Type typeToConvert,
			JsonSerializerOptions options)
		{
			var dto = JsonSerializer.Deserialize<ContactTargetDto>(ref reader, options)
				?? throw new InvalidDataException("Contact target payload is missing.");
			return dto.Kind switch
			{
				"fleet" => new FleetContactTarget(dto.Id),
				"wreck" => new WreckContactTarget(dto.Id),
				_ => throw new InvalidDataException($"Unknown contact target '{dto.Kind}'."),
			};
		}

		public override void Write(
			Utf8JsonWriter writer,
			ContactTarget value,
			JsonSerializerOptions options)
		{
			var dto = value switch
			{
				FleetContactTarget fleet => new ContactTargetDto("fleet", fleet.UnitId),
				WreckContactTarget wreck => new ContactTargetDto("wreck", wreck.ContractId),
				_ => throw new InvalidDataException(
					$"Unsupported contact target '{value.GetType().Name}'."),
			};
			JsonSerializer.Serialize(writer, dto, options);
		}
	}

	private sealed class WreckageOutcomeJsonConverter : JsonConverter<WreckageOutcome>
	{
		public override WreckageOutcome Read(
			ref Utf8JsonReader reader,
			Type typeToConvert,
			JsonSerializerOptions options)
		{
			using var document = JsonDocument.ParseValue(ref reader);
			var value = document.RootElement;
			if (value.TryGetProperty("loot", out var loot))
				return new WreckageOutcome.Salvage(
					JsonSerializer.Deserialize<ResourceBundle>(loot, options)
					?? throw new InvalidDataException("Wreckage salvage loot is missing."));

			if (value.TryGetProperty("fleet", out var fleet))
				return new WreckageOutcome.Ambush(
					JsonSerializer.Deserialize<FleetSpawnSpec>(fleet, options)
					?? throw new InvalidDataException("Wreckage ambush fleet is missing."));

			throw new InvalidDataException(
				"Wreckage outcome must contain either 'loot' or 'fleet'.");
		}

		public override void Write(
			Utf8JsonWriter writer,
			WreckageOutcome value,
			JsonSerializerOptions options)
		{
			writer.WriteStartObject();
			switch (value)
			{
				case WreckageOutcome.Salvage salvage:
					writer.WritePropertyName("loot");
					JsonSerializer.Serialize(writer, salvage.Loot, options);
					break;
				case WreckageOutcome.Ambush ambush:
					writer.WritePropertyName("fleet");
					JsonSerializer.Serialize(writer, ambush.Fleet, options);
					break;
				default:
					throw new InvalidDataException(
						$"Unsupported wreckage outcome '{value.GetType().FullName}'.");
			}
			writer.WriteEndObject();
		}
	}

}

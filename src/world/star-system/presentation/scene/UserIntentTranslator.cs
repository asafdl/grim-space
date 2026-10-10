using Godot;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Ids;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Agents;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Presentation.Camera;
using GrimSpace.World.StarSystem.Presentation.Diagnostics;
using GrimSpace.World.StarSystem.Presentation.Picking;

namespace GrimSpace.World.StarSystem.Presentation.Scene;

public sealed class UserIntentTranslator
{
	private readonly StarMapPlayerExecutionAgent? _playerAgent;
	private readonly Func<Coord, Coord>? _resolveDestination;
	private readonly Func<MapInteractiveTarget>? _resolveTarget;
	private readonly string? _actorId;
	private readonly IImmediateActionSink? _immediateActions;
	private Vector2? _lmbPressPosition;
	private MapInteractiveTarget? _lmbPressUnit;

	public UserIntentTranslator(
		StarMapPlayerExecutionAgent playerAgent,
		Func<Coord, Coord>? resolveDestination,
		Func<MapInteractiveTarget> resolveTarget)
	{
		_playerAgent = playerAgent;
		_resolveDestination = resolveDestination;
		_resolveTarget = resolveTarget;
	}

	public UserIntentTranslator(string actorId, IImmediateActionSink immediateActions)
	{
		ArgumentException.ThrowIfNullOrEmpty(actorId);
		ArgumentNullException.ThrowIfNull(immediateActions);
		_actorId = actorId;
		_immediateActions = immediateActions;
	}

	public bool TryAcceptContract(
		string poiId,
		string facilityId,
		string operatorName,
		string contractId)
	{
		return TrySubmitImmediately(new AcceptContractAction(
			FacilityActorId(),
			poiId,
			facilityId,
			operatorName,
			contractId));
	}

	public bool TryDeclineContract(
		string poiId,
		string facilityId,
		string operatorName,
		string contractId) =>
		TrySubmitImmediately(new DeclineContractAction(
			FacilityActorId(),
			poiId,
			facilityId,
			operatorName,
			contractId));

	public bool TryVisitContractMerchant(
		string poiId,
		string facilityId,
		string operatorName) =>
		TrySubmitImmediately(new VisitContractMerchantAction(
			FacilityActorId(),
			poiId,
			facilityId,
			operatorName));

	public bool TryPurchase(
		string poiId,
		string facilityId,
		string operatorName,
		EMerchantCatalog catalog,
		MerchantCatalog.Offering offering,
		ShipInstance before) =>
		TrySubmitImmediately(new PurchaseAction(
			FacilityActorId(),
			poiId,
			facilityId,
			operatorName,
			catalog,
			offering,
			before));

	public bool TryRecruitShip(
		string poiId,
		string facilityId,
		string operatorName,
		ShipRecruitmentCatalog.Offer offer) =>
		TrySubmitImmediately(new RecruitShipAction(
			FacilityActorId(),
			poiId,
			facilityId,
			operatorName,
			new ShipSpawnDeclaration(
				TypedIdGenerator.NextId(UnitTypeSlug.For(offer.Chassis)),
				offer.Chassis,
				offer.GearTier)));

	public bool TryCompleteDeliveryFacilityLeg(
		string poiId,
		string facilityId,
		string operatorName,
		string contractId,
		int legIndex,
		string? meetingId) =>
		TrySubmitImmediately(new CompleteDeliveryFacilityLegAction(
			FacilityActorId(),
			poiId,
			facilityId,
			operatorName,
			contractId,
			legIndex,
			meetingId));

	public bool TryHandleMouseButton(InputEventMouseButton mouseButton, out bool unreachable)
	{
		var resolveTarget = _resolveTarget
			?? throw new InvalidOperationException("Map intents require a target resolver.");
		unreachable = false;
		if (mouseButton.ButtonIndex == MouseButton.Left && mouseButton.Pressed)
		{
			_lmbPressPosition = mouseButton.Position;
			var pressTarget = resolveTarget();
			_lmbPressUnit = pressTarget.Kind == MapInteractiveTargetKind.Unit ? pressTarget : null;
			return true;
		}

		if (mouseButton.ButtonIndex != MouseButton.Left
			|| mouseButton.Pressed
			|| _lmbPressPosition is not { } pressPosition
			|| pressPosition.DistanceTo(mouseButton.Position) >= 4f)
		{
			_lmbPressPosition = null;
			_lmbPressUnit = null;
			return false;
		}

		_lmbPressPosition = null;
		var target = _lmbPressUnit ?? resolveTarget();
		_lmbPressUnit = null;
		var result = TryQueueIntent(target);
		unreachable = result is CourseCommandResult.Unreachable;
		return result is not CourseCommandResult.Ignored;
	}

	public CourseCommandResult TryQueueIntent(MapInteractiveTarget target) =>
		target.Kind switch
		{
			MapInteractiveTargetKind.Unit when target.Unit is { } unit =>
				PlayerAgent().TryQueuePursueFleet(unit.UnitId),
			MapInteractiveTargetKind.Wreck when target.WreckContractId is { } wreckContractId =>
				PlayerAgent().TryQueueWreckContact(wreckContractId),
			MapInteractiveTargetKind.SelfClickNoOp =>
				new CourseCommandResult.SelfClickIgnored(),
			MapInteractiveTargetKind.None
				or MapInteractiveTargetKind.Landmark
				or MapInteractiveTargetKind.Dock
				or MapInteractiveTargetKind.Poi when target.MoveGridPoint is { } pick =>
				PlayerAgent().TryQueueMove(_resolveDestination?.Invoke(pick) ?? pick),
			_ => LogMovePickMissAndIgnore(),
		};

	private StarMapPlayerExecutionAgent PlayerAgent() =>
		_playerAgent
		?? throw new InvalidOperationException("Map intents require a player execution agent.");

	private string FacilityActorId() =>
		_actorId
		?? throw new InvalidOperationException("Facility intents require a player actor.");

	private bool TrySubmitImmediately(IAction action)
	{
		var actions = _immediateActions
			?? throw new InvalidOperationException(
				"Facility intents require an immediate action sink.");
		return actions.TryEnqueue([action]) && actions.CommitImmediately();
	}

	private CourseCommandResult LogMovePickMissAndIgnore()
	{
		StarMapPresentationDiagnostics.LogMovePickMiss();
		return new CourseCommandResult.Ignored();
	}
}

using System.Collections.Generic;
using System.Linq;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Actions.Enums;
using GrimSpace.Battle.Grid;
using GrimSpace.Battle.Units;

namespace GrimSpace.Battle.Movement;

public sealed class Engine
{
	private readonly Dictionary<string, State> _activeShips = new();
	private readonly Dictionary<string, List<ActionRequest>> _committedTurns = new();

	public void RegisterShip(State ship) => _activeShips[ship.Id] = ship;

	public State? GetShip(string shipId) =>
		_activeShips.GetValueOrDefault(shipId);

	public Trajectory GetTrajectoryPreview(State initialState, List<ActionRequest> queuedActions)
	{
		var simulatedShip = initialState.Clone();
		var pathPoints = new List<Coord> { simulatedShip.Position };

		foreach (var action in queuedActions)
			ApplyActionToShip(simulatedShip, action);

		simulatedShip.Position += simulatedShip.Velocity;
		pathPoints.Add(simulatedShip.Position);

		return new Trajectory
		{
			ExpectedFinalState = simulatedShip,
			Path = pathPoints,
		};
	}

	public bool ValidateActions(State ship, List<ActionRequest> queuedActions) =>
		queuedActions.Sum(a => a.ApCost) <= ship.ActionPoints;

	public bool CommitTurn(TurnSubmission submission)
	{
		if (!_activeShips.TryGetValue(submission.ShipId, out var ship))
			return false;

		if (!ValidateActions(ship, submission.QueuedActions))
			return false;

		_committedTurns[submission.ShipId] = submission.QueuedActions;
		return true;
	}

	public void ResolveRound()
	{
		foreach (var (shipId, actions) in _committedTurns)
		{
			var ship = _activeShips[shipId];

			foreach (var action in actions)
				ApplyActionToShip(ship, action);

			ship.Position += ship.Velocity;
			ship.ActionPoints = ship.Stats.MaxAp;
		}

		_committedTurns.Clear();
	}

	private static void ApplyActionToShip(State ship, ActionRequest action)
	{
		switch (action.Type)
		{
			case EActionType.Pivot:
				ship.ForwardDirection = action.NewForwardDirection;
				ship.UpDirection = action.NewUpDirection;
				ship.RecalculateRightDirection();
				break;

			case EActionType.MainBurn:
				ship.Velocity += ship.ForwardDirection * ship.Stats.MainThrustPower;
				break;

			case EActionType.RetroBurn:
				ship.Velocity -= ship.ForwardDirection * ship.Stats.RetroThrustPower;
				break;

			case EActionType.LateralThrust:
				ApplyLateralThrust(ship, action.LatDirection);
				break;
		}
	}

	private static void ApplyLateralThrust(State ship, ELateralDirection direction)
	{
		var power = ship.Stats.LateralThrustPower;

		switch (direction)
		{
			case ELateralDirection.Dorsal:
				ship.Velocity += ship.UpDirection * power;
				break;
			case ELateralDirection.Ventral:
				ship.Velocity -= ship.UpDirection * power;
				break;
			case ELateralDirection.Starboard:
				ship.Velocity += ship.RightDirection * power;
				break;
			case ELateralDirection.Port:
				ship.Velocity -= ship.RightDirection * power;
				break;
		}
	}
}

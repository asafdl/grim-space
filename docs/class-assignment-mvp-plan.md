# MVP: class-compatible pilot assignment

Status: domain class data, persistence policy, roster enforcement, and the
approved minimal Fleet Management presentation are implemented. Automated
tests and build pass; manual Godot interaction review remains.

This is the next small experiment, not the first phase of a committed overhaul.
The [larger implementation plan](pilots-and-ships-implementation-plan.md) is
background context, not the execution sequence for this MVP.

## Outcome

In the existing fleet-management screen:

1. A pilot visibly has a class.
2. A ship visibly has a class tag.
3. Compatible assignments work.
4. Incompatible assignments are rejected without mutating state.

Ship class means operator qualification, not equipment quality. There is no
pilot-level gate in this MVP.

## Scope decisions before coding

Agree on a small classification table:

| Content | Proposal |
|---------|----------|
| Starter pilot | Fighter |
| Fighter chassis | Fighter |
| Existing recruited pilots | Fighter for this experiment |
| Other chassis | Explicitly unclassified and unavailable to Fighter pilots |

Unclassified is not a wildcard, a second playable class, or a default Fighter.
Unknown serialized class values must not silently become valid qualifications.
No Support content is required.

This proposal intentionally makes the current bundled Repurposed Miner hire
incompatible with its Fighter pilot. If approved, keep the hire unassigned and
inactive rather than creating an illegal pairing or failing after payment.
Do not redesign the hire transaction here: it can still grant the pilot and
reserve ship. The temporary usability gap must be reported and visible.

Approved decisions: use this classification table, keep the incompatible hire
unassigned and inactive, and reject saves from earlier format versions rather
than migrating or defaulting missing pilot classes.

Unclassified ships remain acquirable and player-owned. They are reserve ships,
not fielded fleet members; only compatible pilots that are assigned and active
are fielded. Acquisition is not rejected because of class.

Confirm this classification and temporary hiring behavior before implementation.
If the broken hire flow makes the experiment impractical, choose an explicit
temporary classification instead; do not silently relax compatibility.

## Implementation

### 1. Add authoritative class data

- Add a domain class identifier with Fighter and an explicit unclassified state.
- Store pilot qualification on [Pilot](../src/run/Pilot.cs), preserving it through
  recruit materialization, copies, XP updates, and other immutable updates.
- Carry it through [PilotRecruitProfile](../src/pilots/PilotRecruitProfile.cs).
- Put the ship class tag on the existing chassis definition boundary,
  [ShipSpec](../src/units/specs/ShipSpec.cs), with explicit mappings for existing
  chassis. Do not duplicate it on mutable loadouts or individual ship instances.
- Preserve pilot class through save/load using the existing persistence system.
  Keep this limited to the new field: no gear-tier schema work.
- Before changing save handling, choose either a narrow legacy-class migration
  consistent with the approved table or explicit version incompatibility.
  Reject restored illegal pairings clearly; do not silently reclassify them.

### 2. Enforce compatibility at the roster boundary

Use the existing [RunPilotRegistry](../src/run/RunPilotRegistry.cs) and
[Run.State](../src/run/State.cs) boundaries, not a new assignment service.

- Establish one shared class-compatibility rule consumed by authoritative
  mutations and presentation queries.
- Apply it to direct assignment, the current assignment/swap flow, public swap
  operations, and activation. Trace all callers before modifying signatures.
- Validate both resulting pairings before a swap mutates anything.
- Unassigned pilots remain valid; an active pilot must have a legal ship.
- Retain ownership checks, exclusive occupancy, battle locks, and active limits.
- Preserve existing successful assignment and active-order behavior; do not
  introduce the separate "replace an active pilot's equipment" redesign here.
- Ensure creation/acquisition and restoration paths cannot register an active
  incompatible pair. Change only the directly coupled setup needed to obey the
  invariant, including the approved temporary hire behavior.
- Extend the existing assignment result with a meaningful failure reason.
  Keep domain results independent of UI strings and Godot.

Example user-facing explanation:
"Fighter pilots cannot operate an unclassified ship."

Other assignment failures must retain their own explanation rather than being
misreported as class mismatches. Rejection must leave assignments, active order,
strategic membership, and capability state unchanged.

### 3. Expose the experiment in the existing UI

- Extend [FleetManagementProjection](../src/world/star-system/presentation/ui/FleetManagementProjection.cs)
  with pilot and ship class labels and assignment eligibility/rejection data.
- Display the Fighter class icon next to the pilot background icon, with visible
  class text; show the selected ship's class as a row in its existing stats card.
- Keep the existing pilot picker layout without inline eligibility explanations.
- Rejected assignment requests do not show player-facing feedback.
- Reuse existing widgets and refresh flow. No character-sheet, shop, or HUD
  redesign is required.

### 4. Supply a repeatable demonstration and targeted tests

Use existing dev fixtures/preview infrastructure to provide:

- A Fighter pilot and compatible Fighter ship.
- An owned non-Fighter/unclassified ship to demonstrate rejection.
- Enough pilots/ships to exercise a rejected swap.

Do not require new merchant access or change enemy battle generation to make
the test setup available.

Extend existing tests rather than adding a second harness:

- Class survives pilot updates and the chosen save/load policy.
- Compatible assignment succeeds.
- Unclassified/wrong-class assignment rejects with the expected reason.
- Invalid swaps validate both legs and leave all state unchanged.
- Direct roster methods and activation cannot bypass compatibility.
- Acquisition does not auto-activate an incompatible bundled hire.
- Existing ownership, occupancy, and battle-lock behavior remains intact.
- Projection exposes both labels and the correct rejection reason.

Use pure C# fixtures for rule/projection tests, not Godot node unit tests.
Run the smallest relevant test selection from
`dotnet test grim-space.Tests/grim-space.Tests.csproj`, plus `dotnet build`.
Check labels and rejection feedback manually in the repeatable Godot setup.

## Explicitly out of scope

- Support class or a second playable class/toolkit.
- Equipment tiers, deterministic tier packages, and minimum pilot levels.
- Merchant consolidation, prices, stock, or removal of upgrade purchases.
- Separating bundled hiring into pilot-only transactions.
- New skills, quirk generation, perk retargeting, or balance changes.
- Changing successful assignment semantics beyond class enforcement.
- Broad cleanup of half-finished systems.

## Acceptance and review

The MVP is complete when the four visible outcomes at the top work in the
repeatable setup, the directly affected invariants are tested, and runtime state
and class data survive the selected persistence policy.

Report any temporarily broken campaign flows explicitly. Temporary incompleteness
is acceptable; crashes, partial assignment mutations, silent fallback classes,
and success-shaped failure are not.

Then stop and review the actual interaction with the developer. Choose the next
MVP from that feedback rather than proceeding automatically to merchants or
equipment progression.

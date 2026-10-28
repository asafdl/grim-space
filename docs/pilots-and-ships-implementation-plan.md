# Fighter equipment progression: implementation plan

Status: proposed plan only. No runtime changes are authorized by this document.

Related design: [Pilot classes and replaceable ships](pilots-and-ships-design.md).

## Current scope

Start with **one player class: Fighter**, using Fighter ships. Support is a
possible second class, but no Support ship, placeholder toolkit, skill tree, or
content is part of this implementation.

The first release establishes:

- Fighter-class pilots and compatible Fighter equipment.
- Explicit equipment tiers with minimum earned pilot levels.
- Whole-ship purchases instead of incremental hardware upgrades.
- Buying ahead, with a warning when no owned pilot can operate the ship.
- Separate pilot recruitment and ship purchasing.
- Correct assignment, fielding, maintenance, and persistence.

This is an equipment/acquisition overhaul, not a simultaneous redesign of combat,
pilot progression, every background, or every merchant. Existing enemy chassis
and their generation remain available.

## Rules to preserve throughout implementation

1. Run state owns cross-system coordination; the pilot registry owns roster and
   assignments; item inventory owns ship instances.
2. Class and level eligibility are domain/run rules. UI filtering is not their
   enforcement boundary.
3. Ship operation requires the assigned pilot's own earned level, derived from
   XP, not claimed level or the best pilot in the roster.
4. Purchasing does not require a qualified pilot and does not assign equipment.
5. Player equipment tiers have no run progression gate. Merchant stock is not
   gated by contract-completion shop tiers; ship brokers list every configured
   equipment tier and pilot brokers draw from the full recruitable background pool.
6. Pilot capabilities remain derived and follow the pilot, without being baked
   into a ship's permanent hardware.
7. Existing maintenance, payroll, battle loss rules, and squad-size limit remain
   unchanged unless an explicit follow-up decision changes them.
8. Reuse existing actions, committed records, inventory, and capability paths.
   Do not add a parallel equipment service or a second party model.

## Decisions required before runtime implementation

Prepare a concrete proposal for approval covering:

| Decision | Required detail |
|----------|-----------------|
| Fighter baseline | Confirm the existing starting Fighter package or specify changes; do not assume every supported mount is installed |
| Initial tier ladder | Recommend two tiers for this slice; approve exact names and hardware values |
| Level requirements | Approve the minimum earned level for each tier; baseline equipment must support a new pilot |
| Tier identity | Decide how purchased package/tier identity is retained on an owned ship and through save/load |
| Prices | Approve ship costs and resource types; current individual-upgrade prices are not automatically new ship prices |
| Stock | Ship brokers list all approved Fighter tiers at once; pilot hire uses the full recruitable background pool |
| Hiring | Confirm pilot-only hire price now that the bundled ship is removed |
| Obsolete perk | Decide whether to retire or redesign "I Know a Bot"; no implicit purchase-discount conversion |
| Save policy | Choose explicit incompatibility or a defined migration for old classes, ships, and learned obsolete perks |

These are not reasons to build a configurable framework. Prefer the smallest
catalog/model extension that expresses the approved packages and requirements.

## Execution and approval checkpoints

Execute one numbered step at a time. Include directly coupled tests and
documentation in each step, report the result, and stop for feedback before
starting the next. The final validation step is not a substitute for testing
earlier changes.

### 1. Finalize the contract and audit affected entry points

Deliver the concrete decisions above before editing runtime code.

Trace existing entry points for ship creation, pilot creation, assignment,
activation, deployment, merchant quotes/purchases, and save restoration.
Distinguish player paths from shared enemy/fixture paths.

Primary surfaces:

- [Pilot](../src/run/Pilot.cs), [PilotRecruitProfile](../src/pilots/PilotRecruitProfile.cs),
  and [RunPilotRegistry](../src/run/RunPilotRegistry.cs).
- [ShipInstance](../src/units/ShipInstance.cs),
  [ShipSpawnDeclaration](../src/units/ShipSpawnDeclaration.cs),
  [ShipCatalog](../src/units/ShipCatalog.cs), and
  [ShipLoadoutTierRoller](../src/units/ShipLoadoutTierRoller.cs).
- [Run.State](../src/run/State.cs), acquisition records, and persistence DTOs.

Call out encountered TODO debt in the affected flow; address it only when needed
for this slice. Resolve save policy and shared-caller risks before model changes.

Checkpoint: approved package table, level thresholds, pricing, perk disposition,
save policy, and bounded implementation surface.

### 2. Establish Fighter qualification and explicit equipment packages

- Add Fighter class qualification to the pilot model and recruit profile.
- Describe approved player Fighter packages with tier identity, minimum pilot
  level, fixed ability arrangement, and explicit hardware values.
- Keep chassis/spec, equipment quality, current condition, and pilot level
  separate. Do not model a higher tier as a different pilot or a mutable class.
- Ensure preview, purchase, and instance creation resolve the same package.
- Keep the class's baseline ability package unchanged between these first tiers.
- Do not infer identity or requirements from current shields, damage, or hull.
- Avoid globally replacing the shared tier roller: preserve enemy generation
  unless a specific shared change is required and tested.
- Carry new identity through clone, creation, and persistence paths; implement
  the agreed version/migration policy here so the model change is not lossy.
- Give new-run pilots and starter ships valid baseline values.

Tests: exact package output, independent instance condition, identity surviving
clone/save/load, valid baseline starter, and unchanged shared/enemy tier behavior.

Checkpoint: the approved equipment ladder exists as authoritative data; no
merchant conversion yet.

### 3. Enforce operation requirements and support ship replacement

- Extend existing authoritative assignment and activation rules with class and
  minimum earned-level checks.
- Add the explicit operation "equip this pilot with this owned ship" using the
  existing run/registry boundary.
- Preserve an active pilot's squad position when equipping an empty reserve ship.
- Keep the replaced ship in inventory as reserve equipment.
- Validate both resulting assignments for swaps before mutating either.
- Reject invalid assignments without partial roster/inventory changes.
- Retain ownership checks, exclusive occupancy, maximum active count, and the
  restriction against reassignment during an active battle.
- Refresh strategic membership, selected member, and capability projections.
- Ensure activation, deployment, and restored state cannot bypass requirements;
  surface invalid restored state according to the agreed save/error policy.
- Return enough failure information for existing presentation error mechanisms
  to distinguish class, level, ownership, and battle-lock failures.

Tests: level immediately below/at threshold, earned versus claimed level, wrong
class, qualified reserve pilot, active replacement order, valid/invalid swaps,
no partial mutation, battle lock, and capability movement without duplication.
Use fixtures for unavailable future classes; do not create Support content just
to test rejection.

Checkpoint: a pilot can legally change equipment and retain progression and
fielding independently of merchant work.

### 4. Replace player hardware-upgrade commerce with ship purchases

- Change player ship listings to the approved Fighter packages and prices.
- List every approved Fighter package on ship brokers; remove legacy merchant
  availability-band filtering from listings, purchase validation, and UI subtitles.
- Retain endpoint/access, ownership/duplicate identity, engagement, affordability,
  and existing transaction checks.
- Keep buying legal without any qualified pilot, and register purchases as
  unoccupied owned equipment.
- Prevent caller-supplied declarations from obtaining unquoted hardware: the
  purchased package must match the validated offer.
- Remove damage, range, and maximum-shield upgrade listings and reject those
  obsolete player transactions at the authoritative purchase boundary.
- Do not introduce permanent maximum-hull upgrade purchases.
- Retain hull repair, shield recharge, and fuel services and their resource flow.
- Adjust weapon/support merchant stock and routing affected by removal; do not
  leave destinations advertising nonexistent services.
- Apply the approved disposition for upgrade-only perks and saved learned
  versions of them; do not leave selectable no-op skills.

Primary surfaces: [StarMarketSeed](../src/world/star-system/merchants/StarMarketSeed.cs),
[MerchantCatalog](../src/world/star-system/merchants/MerchantCatalog.cs),
[PurchaseShipAction](../src/world/star-system/actions/PurchaseShipAction.cs),
generic purchase validation, merchant stock/routing, and upgrade-capability code.
Internal loadout-construction helpers can remain where still useful.

Tests: exact stock/costs, all tiers visible from run start, buying with no pilots, exact purchased package, insufficient funds, stale/invalid
offers, duplicate IDs, rejected direct upgrade requests, maintenance preserving
maximum hardware values, and unaffected unrelated offers.

Checkpoint: permanent player hardware progression is exclusively whole-ship
acquisition, with maintenance still functional.

### 5. Separate Fighter pilot hiring from equipment acquisition

- Generate Fighter-qualified candidates using existing deterministic lead slots.
- Remove ship declarations from recruitment candidates and committed hire facts.
- Update action resolution and run consumers together: pay once, consume the
  slot once, and register only the pilot.
- Keep a new hire unassigned until the player explicitly equips/fields them.
- Preserve identity, backgrounds, existing working quirks/skills, salary, hazard
  pay, and reserve payroll rules; no new skill-tree or quirk-roll redesign.
- Update affected fixtures and dev acquisition paths to obey the player model,
  without constraining enemy construction.

Tests: deterministic candidates, pilot-only inventory delta, no automatic
activation, consumed-slot behavior, affordability, full active roster, reserve
payroll participation, and save/load of an unassigned hire.

Checkpoint: pilots and ships can be acquired independently and combined through
the assignment rules established in step 3.

### 6. Complete player-facing integration

- Ship shop: show class, tier, minimum earned level, fixed toolkit, actual
  hardware values, and price.
- Compare hardware independently of pilot bonuses.
- Evaluate qualification across all owned pilots, including reserves.
- Show an explicit pre-purchase warning when none qualify; allow purchase
  anyway. Do not warn merely because qualified pilots are inactive or occupied.
- Roster/assignment: show equipment tier separately from pilot level, explain
  ineligibility, expose owned reserve ships and unassigned pilots, and make
  displacement/replacement explicit.
- Hiring: remove bundled-ship language; show class, price, ongoing costs, and
  whether currently owned equipment is usable by the candidate.
- Update affected merchant labels, stock states, previews, tutorials, and dev
  screens so they do not teach incremental hardware upgrades.
- Keep scope to existing screens/components rather than a wholesale HUD redesign.

Tests: qualification/warning projections, threshold updates after gaining XP,
reserve-roster handling, display of package values, removed upgrade routing, and
error messages. Validate visuals separately in Godot; do not unit-test framework
nodes.

Checkpoint: the complete loop is usable without dev tooling or misleading UI.

### 7. Validate the integrated loop and document the new behavior

Run a repeatable scenario:

1. Start a run with a qualified Fighter pilot and baseline Fighter.
2. Buy a better Fighter before the pilot reaches its requirement; see the warning.
3. Confirm purchase succeeds but assignment/use is rejected.
4. Earn enough XP; confirm the same pilot now qualifies without any merchant unlock.
5. Equip the new ship; retain pilot progression, active position, and old reserve
   ship, and confirm the next battle uses the correct package and capabilities.
6. Hire another Fighter pilot without receiving hardware; equip a legal reserve
   ship and field them explicitly.
7. Repair/recharge equipment without increasing base capacities.
8. Save/load across unassigned, pre-purchased, and reassigned states.

Use the existing test project and targeted selectors for each earlier step.
For final cross-system verification, run:

- `dotnet build`
- `dotnet test grim-space.Tests/grim-space.Tests.csproj`

Update the architecture/run-ship documentation and this design's current scope
to match what actually shipped. Confirm old-save behavior is explicit, removed
offers cannot be invoked directly, and no unrelated player/enemy systems changed.

Checkpoint: report verified outcomes, remaining balance questions, and any
encountered debt. Do not expand to Support automatically.

## Existing tests to extend

Prefer existing suites instead of introducing a parallel test harness:

- Units: ship catalog tier output, loadout tier identity, and instance behavior.
- Run: pilot registry, pilot acquisition, ship acquisition, party membership,
  battle spawn capabilities, progression, destruction, and persistence.
- Merchants: ship catalog/purchases, pilot recruitment/hiring, generic purchase
  validation, stock/routing, and maintenance catalogs.
- Presentation projections: price/package displays, eligibility, and warnings.

Rewrite tests asserting intentionally removed player purchases; retain coverage
for still-used low-level construction and enemy behavior.

## Deferred work

- Support class and any second player chassis.
- Extensive class-specific skill trees, new backgrounds, or quirk content.
- Resale, trade-ins, salvage economy, or automatic ship disposal.
- Modular fitting, random player equipment affixes, and equipment sidegrades.
- New escape/recovery rules or changes to pilot death on ship destruction.
- General merchant-framework or combat-engine rewrites.

This slice verifies equipment progression and acquisition correctness. A single
class does not yet validate cross-class squad composition or prove that
within-class recruitment has sufficient long-term depth.

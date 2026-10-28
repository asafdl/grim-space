# Pilot classes and replaceable ships

Status: design proposal, not implemented.

Implementation scope update: begin with Fighter-class pilots and Fighter ships
only; Support is deferred. The two-class experiment below remains a later design
target, not the scope of the first implementation. See the
[implementation plan](pilots-and-ships-implementation-plan.md) for sequencing,
approval checkpoints, and unresolved implementation decisions.

## Goal

Make hiring and developing a roster meaningful while simplifying ship progression.
Pilots are persistent characters; ships are replaceable equipment. A ship's class
provides its baseline tactical toolkit, its equipment tier provides hardware
quality, and its pilot's skills and quirks provide specialization.

The initial experiment has **two player classes, both TBD**. This document does
not choose their names, chassis, abilities, or balance values.

This is not intended to reproduce Battle Brothers exactly. It combines
class-bound recruitment with the equipment progression of replacing a weapon
with a better version, rather than repeatedly improving the same item.

## Core decisions

| Concern | Proposed approach |
|---------|-------------------|
| Player classes | Two initially; identities and toolkits TBD |
| Pilot qualification | Each pilot belongs to one class and can operate only compatible ships whose minimum pilot level they meet |
| Baseline combat abilities | Supplied by the ship's class toolkit |
| Pilot differentiation | Skill choices, progression, and quirks within that class |
| Hardware progression | Acquire a higher-tier ship instance |
| Equipment access | Pilot level gates operating better ships, not run progression bands; advance purchases are allowed |
| Individual hardware upgrades | No player purchases that improve an existing ship's damage, range, maximum shields, or maximum hull |
| Pilot hiring | Recruit a person independently of acquiring a ship |
| Ship acquisition | Purchase equipment independently of recruiting a pilot |
| Maintenance | Repair hull and recharge shields; these restore condition, not equipment quality |
| Enemy variety | Not restricted to the two player classes |

The earlier idea of a generic fighter receiving a pilot-owned launcher or goop
gun is not the baseline for this proposal. Here, class ships supply their
toolkits; pilot choices specialize their use. Whether any later skill grants an
additional action is a separate content decision.

## Progression and identity

### Pilots

A pilot owns:

- Identity and class qualification.
- XP, level, and learned skill choices.
- Quirks and background.
- Salary and existing payroll consequences.

These remain with the pilot when changing to another compatible ship. Buying
better hardware does not grant pilot levels or reset development.

Pilots of the same class must have meaningful differences. The desired question
is not only "do I need this class?" but also "which recruit of this class do I
want to develop?" A small set of working choices is preferable to a large tree
of weak bonuses or unimplemented descriptions.

Exact skill structure, class-specific versus shared skills, quirk generation,
and starting recruit experience remain TBD. This proposal does not require
building a new skill-tree framework before testing the loop.

### Ships

A ship is an individually owned equipment instance with:

- A compatible class and an equipment tier.
- A minimum pilot level required to operate it.
- The class's baseline ability package.
- Hardware values such as damage, maximum hull, and directional shield capacity.
- Current condition, including hull damage and shield depletion.

A higher-tier ship is the equivalent of an upgraded weapon: better hardware in
the same tactical role, not a new character class. For the first experiment,
keep the ability package and mounting arrangement stable within each class
across tiers. Increase explicitly defined hardware values rather than adding
unrelated abilities as an incidental result of tier generation.

Not every value must increase at every tier. The exact stat increases are TBD;
offers must state their actual improvements. Range or mobility changes need
particular care because they can change tactical reach rather than just power.

Equipment tier describes hardware quality; pilot level determines whether an
individual pilot is qualified to operate it. Higher tiers require higher pilot
levels, with exact thresholds TBD. Use the earned level derived from pilot XP,
not the separate claimed-level field used by existing progression/payroll flows.

Player ship equipment tiers are not unlocked by run progression or merchant
availability bands. Do not retain the current band gate alongside the new level
requirement. Merchant stock and affordability can still affect acquisition, but
the equipment progression gate is the assigned pilot's own class and level,
not the highest level reached by anyone in the roster.

Buying ahead is allowed even if nobody currently qualifies. Before purchase,
show the class and minimum pilot level and, if no owned pilot meets both
requirements, an explicit warning: "No current pilot can operate this ship."
This warning does not block purchase; the ship remains reserve equipment until
a qualified pilot is assigned. Check the entire roster, including reserves.

### Where an improvement belongs

| Improvement | Owner |
|-------------|-------|
| Higher base weapon damage | Higher-tier ship |
| More base maximum hull or shield capacity | Higher-tier ship |
| Restoring lost hull or shields | Maintenance of the current ship |
| Learned conditional damage, defense, or handling benefit | Pilot skill |
| Personal strength or drawback | Pilot quirk/background |
| Changing baseline class toolkit | Class design, not an ordinary tier upgrade |

A pilot skill may affect effective damage or survivability. That must remain a
pilot-derived capability, not a permanent mutation of the assigned ship's base
loadout. Moving the pilot must not leave the old ship with their bonuses.

## Hiring and acquisition loop

1. Review recruits, including class, meaningful traits, development, hiring cost,
   and ongoing salary.
2. Hire a pilot into the roster without receiving an automatic ship.
3. Acquire a compatible ship separately, or assign a compatible ship already
   owned.
4. Field the pilot/ship pairing and earn pilot progression through play.
5. Spend earned resources on maintenance, recruitment, or better ships.
6. Assign a higher-tier compatible ship once the pilot meets its minimum level,
   retaining the pilot's identity and development. The ship may be bought ahead
   of that point.

An unassigned pilot and an unoccupied ship are valid reserve states. Recruitment
must clearly indicate whether a compatible ship is available and whether there
is room in the fielded squad. A ship purchase must not silently create a pilot.

Use "hire pilot" and "buy ship" in player-facing language. Existing code calls
some ship offers recruitment; that should not imply ships are personnel.

### Replacement and assignment

Purchasing a ship and assigning it are distinct decisions. Buying better
hardware should not implicitly discard an existing ship or replace a pilot.
Assignment must enforce class compatibility, the receiving pilot's minimum
level, and exclusive occupancy, and make any displacement or swap explicit.
Validate both resulting pairings in a swap; a qualified pilot must not indirectly
equip an under-level pilot with restricted hardware.

The design must support replacing the ship of a fielded pilot while keeping
that pilot fielded in the intended squad position. This is different from
placing a different pilot into a currently fielded ship.

For the first experiment, retain an old ship as reserve equipment rather than
requiring a sale system. Resale, trade-ins, salvage value, and inventory limits
remain TBD. No automatic transfer of old ship upgrades is needed because the
new progression loop does not sell those upgrades.

### Economic consequences

Higher-tier purchases become a major progression expense. Revisit their prices
and availability after removing the existing incremental upgrade purchases;
do not simply retain both expense curves.

Strictly better tiers mainly create economic decisions: whether to buy now,
save for another purchase, or equip one pilot before another. That is
intentional for this experiment, not a promise of extensive equipment
sidegrades.

Keep the existing distinction between hiring cost, salary, and hazard pay.
The current salary roster includes reserve pilots, so hiring someone before
acquiring their ship has an ongoing cost unless deliberately redesigned.
Retain existing payroll rules for the experiment; make this consequence visible.

The opening roster must have compatible equipment, and early recruitment and
ship availability must make assembling a usable squad feasible. Exact starting
resources, stock, and safeguards against an unusable roster remain balance and
implementation decisions.

## Existing build and required changes

### State ownership already supports much of the separation

[Run.State](../src/run/State.cs) coordinates the pilot roster and item inventory.
[RunPilotRegistry](../src/run/RunPilotRegistry.cs) owns pilot registration,
assignment, and active order. Ships live in the run
[Inventory](../src/run/items/Inventory.cs). The fielded squad is derived from up
to four active pilots and their assigned ship IDs.

Keep these authorities. Do not introduce a second party list or a UI-owned
class/loadout registry. Class and minimum-level requirements must be enforced at
authoritative assignment and activation/deployment boundaries, not only by
filtering a picker. Purchase legality must not impose a qualified-pilot
requirement.

### Pilot hiring currently bundles hardware

[PilotRecruitment](../src/world/star-system/merchants/PilotRecruitment.cs)
generates three deterministic candidates per lead. Every candidate currently
includes a T0 Repurposed Miner, begins with zero XP, and receives no rolled
learned skills or quirks. Background and salary vary; the hiring price is fixed.

[HirePilotAction](../src/world/star-system/actions/HirePilotAction.cs) charges
resources, consumes the lead's slot, and emits a
[PilotHireCommitted](../src/world/star-system/merchants/PilotHireCommitted.cs)
fact carrying both the recruit profile and ship declaration. The run registers
both and automatically activates the pilot if there is an available slot.

Required direction:

- Recruit profiles expose class qualification.
- Hiring registers a pilot without manufacturing or assigning a ship.
- Preserve deterministic candidates and consumed-slot tracking unless another
  design explicitly replaces the lead-based hiring flow.
- Revise the committed hire payload and its run consumer together.
- Replace unconditional bundled-hire activation with an explicit, legal
  assignment/fielding flow.

### Ship purchasing is already separate

[PurchaseShipAction](../src/world/star-system/actions/PurchaseShipAction.cs)
already purchases an owned ship without a pilot; the run registers it in the
inventory. Reuse this acquisition path rather than adding a parallel service.

[StarMarketSeed](../src/world/star-system/merchants/StarMarketSeed.cs) currently
lists T0 Repurposed Miner and T0 Gunship purchases at different merchant
availability bands. These are not yet a player progression ladder of
higher-equipment-tier ships.

Required direction:

- Player offers cover the two chosen classes and their equipment tiers.
- Remove merchant availability-band restrictions on these player ship tiers;
  pilot-level operating requirements replace that progression gate.
- Purchase previews show class, tier, minimum pilot level, toolkit, hardware
  values, and cost, with a pre-purchase warning if nobody in the roster qualifies.
- Existing enemy and specialized unit content need not be deleted or restricted
  simply because it is outside the initial player selection.

### Gear tiers currently roll combinations of upgrades

[ShipCatalog](../src/units/ShipCatalog.cs) creates tiered loadouts through
[ShipLoadoutTierRoller](../src/units/ShipLoadoutTierRoller.cs). The roller spends
a power budget on installing weapons, increasing damage/range, hull, and shields;
it can therefore change the ability package as well as numerical quality.

That behavior is not equivalent to a predictable "same class, better ship"
progression ladder.

Required direction:

- Define explicit player class/tier packages for the prototype.
- Keep each class recognizable across its tiers.
- Audit shared tier-generation callers before changing them; enemy generation
  may still need the existing behavior.
- Existing loadout/upgrade helpers may still be useful for constructing packages.
  Removing purchasable upgrades does not require deleting those internal tools.

### Merchants: upgrade purchases removed

Damage, range, and maximum-shield upgrade purchases are gone, along with the
Weapons and Ship Support merchants. [MerchantCatalog](../src/world/star-system/merchants/MerchantCatalog.cs)
now lists only hull repair, shield recharge, and fuel. The pilot merchant hires
pilots only; ships are sold by the Ship Broker as Fighter tiers T0/T1/T2 from
[StarMarketSeed](../src/world/star-system/merchants/StarMarketSeed.cs).

### Pilot progression exists, but content needs alignment

[PilotProgression](../src/run/PilotProgression.cs) already grants XP to surviving
player pilots on battle wins. The
[skill catalog](../src/pilots/PilotSkillCatalog.cs),
[background catalog](../src/pilots/PilotBackgroundCatalog.cs), and quirk system
provide existing differentiation surfaces. Several background descriptions
contain explicit TBD effects; do not count them as working differentiation.

Use these existing systems to specialize pilots rather than replacing them
wholesale. Class-specific eligibility and the actual choices remain TBD.

The "I Know a Bot" skill was removed from the skill catalog because its
ship-upgrade discount has no target. Its enum value, percent constants, and the
`ShipUpgradeDiscount` capability remain for a possible rework.

### Assignment behavior needs special attention

The current internal assignment flow preserves fielded ship positions when
swapping pilots. Assigning an active pilot to an empty reserve ship can instead
remove that pilot from the active list.

That behavior does not directly implement "equip this active pilot with a better
ship." Extend the existing run/registry boundary to support that operation
explicitly and refresh strategic fleet membership and pilot capabilities.
Preserve the prohibition on reassignment during an active battle.

### Loss and persistence

Current battle-outcome handling removes a destroyed player ship and its assigned
pilot; travel-starvation destruction also removes the associated pilot.
Replaceable equipment does not imply pilot survival, escape pods, or recovery.
Retain current loss behavior for this experiment unless separately changed.

Save data will need class qualification and an unambiguous representation of
the purchased equipment package/tier. Existing snapshots may contain
individually upgraded ships and pilots without a class. A migration or explicit
save-version incompatibility policy must be chosen before runtime rollout; this
document does not choose how old pilots are classified or old upgrades valued.

## Presentation requirements

- Pilot recruitment: class, implemented traits, relevant skills/progression,
  hiring cost, salary, and compatible-equipment availability.
- Ship shop: class, equipment tier, minimum pilot level, baseline abilities,
  actual stat improvements, cost, and qualified roster members. Warn before
  purchase if no current pilot can operate the ship, without blocking buying it.
- Roster/assignment: clearly distinguish pilot level from ship tier, show
  reserves, explain incompatible assignments, and preview replacement.
- Ship comparison: compare hardware separately from the selected pilot's
  effective bonuses so equipment improvements are not confused with skills.
- Maintenance: use repair/recharge language, never imply permanent upgrading.

The existing fleet-management projection is ship-first and exposes only limited
pilot information. It will need richer roster and compatibility information,
but this document does not prescribe a full screen redesign.

## Smallest useful experiment

- Two player classes, identities and baseline packages TBD.
- Two equipment tiers per class.
- Minimum pilot levels for operating higher-tier ships, with advance purchases
  allowed and no run progression-band gate on equipment tiers.
- Enough pilot skill/quirk variation to compare recruits within the same class.
- Separate pilot recruitment and ship purchasing.
- Compatible assignment, reserves, and active-pilot ship replacement.
- No individual player hardware upgrades; maintenance remains available.
- Existing enemy variety, squad-size limit, XP rewards, and payroll retained
  unless a narrowly identified integration requirement demands a change.

Not required: unrestricted fitting, class switching, cross-class qualifications,
random player ship affixes, a large skill tree, resale, new art, or new enemy
classes. The earlier fighter-only experiment is not a prerequisite.

## Validation criteria

The experiment is successful if:

1. Two recruits of the same class can present a meaningful choice beyond name
   and portrait, with their differences implemented rather than only described.
2. Buying a higher-tier compatible ship improves the same pilot's equipment
   without changing identity, XP, skills, quirks, or squad position.
3. Hiring without a spare ship and buying without a spare pilot are supported,
   visible reserve states, with payroll consequences communicated.
4. Incompatible assignments are rejected by domain/run rules and explained by
   presentation; no ship has multiple assigned pilots.
5. Removed upgrade offers cannot still be purchased through old action paths;
   repair and recharge still work and do not increase hardware capacity.
6. Class abilities remain stable across the two tiers, and offers accurately
   preview the equipment ultimately acquired and used in battle.
7. Pilot bonuses follow the pilot, apply once, and do not permanently improve
   a previous ship.
8. Strategic membership, battle deployment, and save/load agree on the roster,
   assignments, equipment, and effective capabilities.
9. An under-level pilot cannot operate a higher-tier ship through assignment,
   swapping, activation, or deployment, even if another pilot qualifies.
10. Higher-tier ship purchases are not progression-band-gated and remain legal
    without a qualified pilot. The shop warns before such a purchase, checking
    active and reserve pilots against both class and level requirements.

Gameplay evaluation must also check whether hiring and developing pilots makes
repeated use of the class toolkit interesting. Transaction correctness alone
does not establish that the roster loop is fun.

## Open decisions before implementation

- The two classes and their baseline ship packages.
- Exact tier names, stats, prices, minimum pilot levels, and merchant stock
  (without run progression-band unlocks for equipment tiers).
- Pilot skill choices, class eligibility, and recruit quirk generation.
- Starting roster/equipment and the early availability of compatible hires
  and ships.
- Replacement or retirement of upgrade-specific pilot perks.
- Save compatibility policy.

Resale/trade-ins, class switching, and new pilot-survival rules are deferred,
not implicit parts of this proposal.
